namespace Umbraco.AI.BrowserAI.Models;

/// <summary>
/// Response body for status endpoint.
/// </summary>
public class StatusResponse
{
    /// <summary>
    /// Whether the Browser AI service is available.
    /// </summary>
    public bool Available { get; set; }

    /// <summary>
    /// Version of the Browser AI provider.
    /// </summary>
    public string Version { get; set; } = "1.0";
}
