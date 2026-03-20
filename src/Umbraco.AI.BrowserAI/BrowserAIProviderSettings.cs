using Umbraco.AI.Core.EditableModels;

namespace Umbraco.AI.BrowserAI;

/// <summary>
/// Settings for the Browser AI provider.
/// </summary>
public class BrowserAIProviderSettings
{
    /// <summary>
    /// Whether the Browser AI provider is enabled.
    /// </summary>
    [AIField]
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Timeout in seconds for waiting for browser response.
    /// </summary>
    [AIField]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum age in seconds for jobs before they are purged.
    /// </summary>
    [AIField]
    public int MaxJobAgeSeconds { get; set; } = 300;

    /// <summary>
    /// Optional fallback provider ID to use when browser AI times out.
    /// </summary>
    [AIField]
    public string? FallbackProviderId { get; set; }
}
