namespace Community.Umbraco.AI.BrowserAI.Models;

/// <summary>
/// Response body for job details sent to browser.
/// </summary>
public class JobResponse
{
    /// <summary>
    /// The job ID.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The prompt text to process.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// The operation type (e.g., "chat", "summarize", "translate").
    /// </summary>
    public string OperationType { get; set; } = string.Empty;
}
