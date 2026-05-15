using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DigiMerchantBE.Options;
using Microsoft.Extensions.Options;

namespace DigiMerchantBE.Middlewares;

public sealed class ApiRequestResponseLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiRequestResponseLoggingMiddleware> _logger;
    private readonly IOptionsMonitor<ApiLoggingOptions> _optionsMonitor;

    public ApiRequestResponseLoggingMiddleware(
        RequestDelegate next,
        ILogger<ApiRequestResponseLoggingMiddleware> logger,
        IOptionsMonitor<ApiLoggingOptions> optionsMonitor)
    {
        _next = next;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var options = _optionsMonitor.CurrentValue;
        var mode = ResolveMode(context, options);
        if (mode == ApiLogMode.None)
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var requestBody = await ReadRequestBodyAsync(context, options.MaxBodyLength);

        if (mode == ApiLogMode.RequestOnly)
        {
            await _next(context);
            stopwatch.Stop();
            var safeRequestBody = SanitizePayload(requestBody, context.Request.ContentType);

            _logger.LogInformation(
                "API REQUEST | {Method} {Path}{Query} | RequestBody={RequestBody} | StatusCode={StatusCode} | ElapsedMs={ElapsedMs}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                safeRequestBody,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);

            return;
        }

        var originalBodyStream = context.Response.Body;
        await using var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        try
        {
            await _next(context);
            stopwatch.Stop();

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var responseBody = await ReadStreamToStringAsync(context.Response.Body, options.MaxBodyLength);
            var safeRequestBody = SanitizePayload(requestBody, context.Request.ContentType);
            var safeResponseBody = SanitizePayload(responseBody, context.Response.ContentType);
            context.Response.Body.Seek(0, SeekOrigin.Begin);
            await context.Response.Body.CopyToAsync(originalBodyStream);

            _logger.LogInformation(
                "API FULL | {Method} {Path}{Query} | RequestBody={RequestBody} | ResponseBody={ResponseBody} | StatusCode={StatusCode} | ElapsedMs={ElapsedMs}",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString,
                safeRequestBody,
                safeResponseBody,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static async Task<string> ReadRequestBodyAsync(HttpContext context, int maxLength)
    {
        if (!CanReadBody(context.Request.ContentType) || context.Request.ContentLength is 0)
        {
            return string.Empty;
        }

        context.Request.EnableBuffering();
        var body = await ReadStreamToStringAsync(context.Request.Body, maxLength);
        context.Request.Body.Position = 0;
        return body;
    }

    private static async Task<string> ReadStreamToStringAsync(Stream stream, int maxLength)
    {
        if (!stream.CanSeek)
        {
            return string.Empty;
        }

        stream.Seek(0, SeekOrigin.Begin);

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var content = await reader.ReadToEndAsync();
        if (content.Length > maxLength)
        {
            return $"{content[..maxLength]}...(truncated)";
        }

        return content;
    }

    private static bool CanReadBody(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        return contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("text/", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("application/xml", StringComparison.OrdinalIgnoreCase)
               || contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizePayload(string payload, string? contentType)
    {
        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(contentType))
        {
            return payload;
        }

        if (!contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
        {
            return payload;
        }

        try
        {
            var root = JsonNode.Parse(payload);
            if (root is null)
            {
                return payload;
            }

            SanitizeNode(root);
            return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
        }
        catch
        {
            return payload;
        }
    }

    private static void SanitizeNode(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            var keys = obj.Select(kv => kv.Key).ToList();
            foreach (var key in keys)
            {
                if (IsSensitiveKey(key))
                {
                    obj[key] = "***REDACTED***";
                    continue;
                }

                if (obj[key] is not null)
                {
                    SanitizeNode(obj[key]!);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not null)
                {
                    SanitizeNode(item);
                }
            }
        }
    }

    private static bool IsSensitiveKey(string key)
    {
        return key.Equals("password", StringComparison.OrdinalIgnoreCase)
               || key.Equals("passwordHash", StringComparison.OrdinalIgnoreCase)
               || key.Equals("rawPassword", StringComparison.OrdinalIgnoreCase)
               || key.Equals("tempPassword", StringComparison.OrdinalIgnoreCase)
               || key.Equals("newPassword", StringComparison.OrdinalIgnoreCase)
               || key.Equals("refreshToken", StringComparison.OrdinalIgnoreCase)
               || key.Equals("accessToken", StringComparison.OrdinalIgnoreCase)
               || key.Equals("k", StringComparison.OrdinalIgnoreCase)
               || key.Equals("d", StringComparison.OrdinalIgnoreCase)
               || key.Equals("token", StringComparison.OrdinalIgnoreCase)
               || key.Equals("authorization", StringComparison.OrdinalIgnoreCase);
    }

    private static ApiLogMode ResolveMode(HttpContext context, ApiLoggingOptions options)
    {
        var rule = options.Rules.FirstOrDefault(r => IsMethodMatch(r.Method, context.Request.Method) && IsPathMatch(r.Path, context.Request.Path));
        var mode = rule?.Mode ?? options.DefaultMode;

        return mode.ToUpperInvariant() switch
        {
            "NONE" => ApiLogMode.None,
            "FULL" => ApiLogMode.Full,
            _ => ApiLogMode.RequestOnly
        };
    }

    private static bool IsMethodMatch(string configuredMethod, string requestMethod)
    {
        if (string.IsNullOrWhiteSpace(configuredMethod) || configuredMethod == "*")
        {
            return true;
        }

        return string.Equals(configuredMethod, requestMethod, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPathMatch(string configuredPath, PathString requestPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath) || configuredPath == "*")
        {
            return true;
        }

        if (configuredPath.EndsWith('*'))
        {
            var prefix = configuredPath[..^1];
            return requestPath.Value?.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) == true;
        }

        return string.Equals(configuredPath, requestPath.Value, StringComparison.OrdinalIgnoreCase);
    }

    private enum ApiLogMode
    {
        None,
        RequestOnly,
        Full
    }
}
