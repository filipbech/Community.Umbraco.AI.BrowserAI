using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Umbraco.AI.BrowserAI;

/// <summary>
/// Background service that periodically purges expired jobs from the store.
/// </summary>
public class BrowserAIJobCleanupService : BackgroundService
{
    private readonly IBrowserAIJobStore _jobStore;
    private readonly IOptions<BrowserAIProviderSettings> _settings;
    private readonly ILogger<BrowserAIJobCleanupService> _logger;

    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserAIJobCleanupService"/> class.
    /// </summary>
    public BrowserAIJobCleanupService(
        IBrowserAIJobStore jobStore,
        IOptions<BrowserAIProviderSettings> settings,
        ILogger<BrowserAIJobCleanupService> logger)
    {
        _jobStore = jobStore;
        _settings = settings;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Browser AI job cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(CleanupInterval, stoppingToken);

                var maxAge = TimeSpan.FromSeconds(_settings.Value.MaxJobAgeSeconds);
                await _jobStore.PurgeExpiredJobsAsync(maxAge);

                _logger.LogDebug("Purged expired Browser AI jobs older than {MaxAge}", maxAge);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purging expired Browser AI jobs");
            }
        }

        _logger.LogInformation("Browser AI job cleanup service stopped");
    }
}
