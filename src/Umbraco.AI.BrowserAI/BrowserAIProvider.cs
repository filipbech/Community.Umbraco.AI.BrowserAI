using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.BrowserAI;

/// <summary>
/// AI provider for Browser-based AI services (Gemini Nano via self.ai).
/// </summary>
/// <remarks>
/// This provider routes inference requests to Chrome's built-in browser AI.
/// The browser runs the model locally; the server acts as a job queue.
/// A Service Worker handles execution so the browser tab does not need to stay open.
/// </remarks>
[AIProvider("browser-ai", "Browser AI (Gemini Nano)")]
public class BrowserAIProvider : AIProviderBase<BrowserAIProviderSettings>
{
    private readonly IBrowserAIJobStore _jobStore;
    private readonly ILogger<BrowserAIProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserAIProvider"/> class.
    /// </summary>
    /// <param name="infrastructure">The provider infrastructure.</param>
    /// <param name="jobStore">The job store for managing browser AI jobs.</param>
    /// <param name="logger">The logger.</param>
    public BrowserAIProvider(
        IAIProviderInfrastructure infrastructure,
        IBrowserAIJobStore jobStore,
        ILogger<BrowserAIProvider> logger)
        : base(infrastructure)
    {
        _jobStore = jobStore;
        _logger = logger;

        WithCapability<BrowserAIChatCapability>();

        _logger.LogInformation("Browser AI provider initialized");
    }

    /// <summary>
    /// Gets the job store for this provider.
    /// </summary>
    internal IBrowserAIJobStore JobStore => _jobStore;

    /// <summary>
    /// Gets the logger for this provider.
    /// </summary>
    internal ILogger<BrowserAIProvider> Logger => _logger;
}
