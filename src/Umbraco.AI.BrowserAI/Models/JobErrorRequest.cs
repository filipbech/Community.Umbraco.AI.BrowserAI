namespace Umbraco.AI.BrowserAI.Models;

/// <summary>
/// Request body for posting job error.
/// </summary>
public class JobErrorRequest
{
    /// <summary>
    /// The error message from browser AI processing.
    /// </summary>
    public string Error { get; set; } = string.Empty;
}
