using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DigiMerchantBE.Common;
using DigiMerchantBE.DTOs.Crypto;
using DigiMerchantBE.Options;
using DigiMerchantBE.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DigiMerchantBE.Services;

public class CryptoEnvelopeService : ICryptoEnvelopeService
{
    private const string SupportedAlg = "RSA-OAEP-SHA256+A256GCM";
    private const int MetadataLength = 60;
    private const int IvLength = 12;
    private const int TagLength = 16;
    private const int KeyLength = 32;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOptionsMonitor<CryptoOptions> _cryptoOptions;
    private readonly IOptionsMonitor<RuntimeOptions> _runtimeOptions;
    private readonly IMemoryCache _memoryCache;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<CryptoEnvelopeService> _logger;

    public CryptoEnvelopeService(
        IOptionsMonitor<CryptoOptions> cryptoOptions,
        IOptionsMonitor<RuntimeOptions> runtimeOptions,
        IMemoryCache memoryCache,
        IWebHostEnvironment environment,
        ILogger<CryptoEnvelopeService> logger)
    {
        _cryptoOptions = cryptoOptions;
        _runtimeOptions = runtimeOptions;
        _memoryCache = memoryCache;
        _environment = environment;
        _logger = logger;
    }

    public async Task<T> ResolvePayloadAsync<T>(
        JsonElement requestBody,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var cryptoOptions = _cryptoOptions.CurrentValue;

        if (CanBypassDecrypt(cryptoOptions))
        {
            var rawPayload = JsonSerializer.Deserialize<T>(requestBody.GetRawText(), JsonOptions);
            if (rawPayload is null)
            {
                throw new ApiException(StatusCodes.Status400BadRequest, "CR017", "Payload raw không hợp lệ");
            }

            return rawPayload;
        }

        var encryptedRequest = JsonSerializer.Deserialize<EncryptedRequestDto>(requestBody.GetRawText(), JsonOptions);
        if (encryptedRequest is null)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR018", "Encrypted envelope không hợp lệ");
        }

        return await DecryptAsync<T>(encryptedRequest, httpContext, cancellationToken);
    }

    public async Task<T> DecryptAsync<T>(
        EncryptedRequestDto request,
        HttpContext httpContext,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateEnvelope(request);

        var options = _cryptoOptions.CurrentValue;
        ValidateTimestampAndNonce(request, options);

        var keyConfig = GetRsaKeyByKid(request.Kid, options);
        var rsaEncryptedBytes = DecodeBase64Strict(request.K, "K");
        byte[] rsaDecryptedBytes;

        using (var rsa = RSA.Create())
        {
            var privateKeyPem = await File.ReadAllTextAsync(ResolvePath(keyConfig.PrivateKeyPemPath), cancellationToken);
            rsa.ImportFromPem(privateKeyPem);
            rsaDecryptedBytes = rsa.Decrypt(rsaEncryptedBytes, RSAEncryptionPadding.OaepSHA256);
        }

        var metadataBytes = ParseMetadataBytes(rsaDecryptedBytes);
        var iv = metadataBytes.AsSpan(0, IvLength).ToArray();
        var tag = metadataBytes.AsSpan(IvLength, TagLength).ToArray();
        var aesKey = metadataBytes.AsSpan(IvLength + TagLength, KeyLength).ToArray();

        var ciphertext = DecodeBase64Strict(request.D, "D");
        var aad = BuildAad(request, httpContext);
        var plaintextBytes = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(aesKey, TagLength);
            aes.Decrypt(iv, ciphertext, tag, plaintextBytes, aad);
        }
        catch (CryptographicException ex)
        {
            _logger.LogWarning(ex, "Failed to decrypt crypto envelope for path {Path}", httpContext.Request.Path);
            throw new ApiException(StatusCodes.Status400BadRequest, "CR006", "Payload mã hóa không hợp lệ");
        }

        var plaintextJson = Encoding.UTF8.GetString(plaintextBytes);
        var payload = JsonSerializer.Deserialize<T>(plaintextJson, JsonOptions);
        if (payload is null)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR007", "Không parse được dữ liệu payload");
        }

        return payload;
    }

    public async Task<IReadOnlyCollection<PublicKeyResponse>> GetPublicKeysAsync(string? kid = null, CancellationToken cancellationToken = default)
    {
        var options = _cryptoOptions.CurrentValue;
        if (options.RsaKeys is null || options.RsaKeys.Count == 0)
        {
            return Array.Empty<PublicKeyResponse>();
        }

        var keys = options.RsaKeys
            .Where(x => string.IsNullOrWhiteSpace(kid) || string.Equals(x.Kid, kid, StringComparison.Ordinal))
            .ToArray();

        var results = new List<PublicKeyResponse>(keys.Length);
        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var publicKeyPath = ResolvePath(key.PublicKeyPemPath);
            if (!File.Exists(publicKeyPath))
            {
                continue;
            }

            var publicKeyPem = await File.ReadAllTextAsync(publicKeyPath, cancellationToken);
            results.Add(new PublicKeyResponse
            {
                Kid = key.Kid,
                IsActive = key.IsActive,
                PublicKeyPem = publicKeyPem
            });
        }

        return results;
    }

    private void ValidateEnvelope(EncryptedRequestDto request)
    {
        if (request is null)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR001", "Payload mã hóa không hợp lệ");
        }

        if (string.IsNullOrWhiteSpace(request.K) || string.IsNullOrWhiteSpace(request.D))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR002", "Thiếu dữ liệu mã hóa");
        }

        if (!string.Equals(request.Alg, SupportedAlg, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR003", "Thuật toán mã hóa không được hỗ trợ");
        }
    }

    private void ValidateTimestampAndNonce(EncryptedRequestDto request, CryptoOptions options)
    {
        if (request.Ts <= 0)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR004", "Timestamp không hợp lệ");
        }

        var replayWindowSeconds = Math.Max(1, options.ReplayWindowSeconds);
        var now = DateTimeOffset.UtcNow;
        var sentAt = DateTimeOffset.FromUnixTimeSeconds(request.Ts);
        var drift = Math.Abs((now - sentAt).TotalSeconds);
        if (drift > replayWindowSeconds)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR005", "Timestamp đã hết hạn");
        }

        if (string.IsNullOrWhiteSpace(request.Nonce))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR008", "Nonce không hợp lệ");
        }

        var nonceCacheKey = $"crypto_nonce:{request.Kid}:{request.Nonce}";
        if (_memoryCache.TryGetValue(nonceCacheKey, out _))
        {
            throw new ApiException(StatusCodes.Status409Conflict, "CR009", "Nonce đã được sử dụng");
        }

        _memoryCache.Set(nonceCacheKey, true, TimeSpan.FromSeconds(replayWindowSeconds));
    }

    private RsaKeyOptions GetRsaKeyByKid(string kid, CryptoOptions options)
    {
        var key = options.RsaKeys.FirstOrDefault(x => string.Equals(x.Kid, kid, StringComparison.Ordinal));
        if (key is null)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR010", "Không tìm thấy key tương ứng với kid");
        }

        if (string.IsNullOrWhiteSpace(key.PrivateKeyPemPath))
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "CR011", "Private key path chưa được cấu hình");
        }

        var privateKeyPath = ResolvePath(key.PrivateKeyPemPath);
        if (!File.Exists(privateKeyPath))
        {
            throw new ApiException(StatusCodes.Status500InternalServerError, "CR012", "Không tìm thấy private key");
        }

        return key;
    }

    private string ResolvePath(string configuredPath)
    {
        return Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(_environment.ContentRootPath, configuredPath);
    }

    private bool CanBypassDecrypt(CryptoOptions cryptoOptions)
    {
        var bypassOptions = cryptoOptions.Bypass ?? new CryptoBypassOptions();
        if (!bypassOptions.EnableRawPayloadBypass)
        {
            return false;
        }

        var configuredEnvironment = _runtimeOptions.CurrentValue.EnvironmentName;
        var environmentName = string.IsNullOrWhiteSpace(configuredEnvironment)
            ? _environment.EnvironmentName
            : configuredEnvironment;

        return string.Equals(environmentName, bypassOptions.AllowedEnvironment, StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] ParseMetadataBytes(byte[] rsaDecryptedBytes)
    {
        if (rsaDecryptedBytes.Length == MetadataLength)
        {
            return rsaDecryptedBytes;
        }

        var asString = Encoding.UTF8.GetString(rsaDecryptedBytes).Trim();
        if (string.IsNullOrWhiteSpace(asString))
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR013", "Metadata mã hóa không hợp lệ");
        }

        var decoded = DecodeBase64Strict(asString, "keys64");
        if (decoded.Length != MetadataLength)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR014", "Metadata mã hóa không đúng độ dài");
        }

        return decoded;
    }

    private static byte[] DecodeBase64Strict(string value, string fieldName)
    {
        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new ApiException(StatusCodes.Status400BadRequest, "CR015", $"{fieldName} không phải Base64 hợp lệ");
        }
    }

    private static byte[] BuildAad(EncryptedRequestDto request, HttpContext httpContext)
    {
        var principal = httpContext.User;
        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? principal.FindFirstValue("nameid")
                     ?? principal.FindFirstValue("sub")
                     ?? string.Empty;
        var jti = principal.FindFirstValue("jti") ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(jti))
        {
            throw new ApiException(StatusCodes.Status401Unauthorized, "CR016", "Token xác thực không hợp lệ");
        }

        var aadString = string.Join('\n',
            httpContext.Request.Method.ToUpperInvariant(),
            httpContext.Request.Path.ToString(),
            request.Ts.ToString(),
            request.Nonce,
            userId,
            jti);

        return Encoding.UTF8.GetBytes(aadString);
    }
}
