using DigiMerchantBE.Options;
using Microsoft.Extensions.Options;

namespace DigiMerchantBE.Services;

public sealed class LogCleanupHostedService : BackgroundService
{
    private readonly IOptionsMonitor<FileLoggingOptions> _loggingOptions;
    private readonly ILogger<LogCleanupHostedService> _logger;
    private readonly IWebHostEnvironment _environment;

    public LogCleanupHostedService(
        IOptionsMonitor<FileLoggingOptions> loggingOptions,
        ILogger<LogCleanupHostedService> logger,
        IWebHostEnvironment environment)
    {
        _loggingOptions = loggingOptions;
        _logger = logger;
        _environment = environment;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await CleanupAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            await CleanupAsync(stoppingToken);
        }
    }

    private Task CleanupAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        var options = _loggingOptions.CurrentValue;
        var retainDays = Math.Max(1, options.RetainDays);
        var cutoff = DateTime.UtcNow.AddDays(-retainDays);

        var fullPath = Path.IsPathRooted(options.FilePath)
            ? options.FilePath
            : Path.Combine(_environment.ContentRootPath, options.FilePath);

        var directory = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return Task.CompletedTask;
        }

        var fileNamePrefix = Path.GetFileNameWithoutExtension(fullPath);
        var extension = Path.GetExtension(fullPath);

        var files = Directory.EnumerateFiles(directory)
            .Where(file =>
            {
                var name = Path.GetFileName(file);
                return name.StartsWith(fileNamePrefix, StringComparison.OrdinalIgnoreCase)
                    && name.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            });

        foreach (var file in files)
        {
            try
            {
                var lastWriteTime = File.GetLastWriteTimeUtc(file);
                if (lastWriteTime < cutoff)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to clean old log file: {FilePath}", file);
            }
        }

        return Task.CompletedTask;
    }
}
