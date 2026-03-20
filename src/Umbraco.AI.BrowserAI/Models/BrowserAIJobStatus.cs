namespace Umbraco.AI.BrowserAI.Models;

/// <summary>
/// Status of a Browser AI job.
/// </summary>
public enum BrowserAIJobStatus
{
    /// <summary>
    /// Job is waiting to be picked up by a browser.
    /// </summary>
    Pending,

    /// <summary>
    /// Job is currently being processed by a browser.
    /// </summary>
    Processing,

    /// <summary>
    /// Job completed successfully.
    /// </summary>
    Complete,

    /// <summary>
    /// Job failed with an error.
    /// </summary>
    Failed,

    /// <summary>
    /// Job timed out waiting for browser response.
    /// </summary>
    TimedOut
}
