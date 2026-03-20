namespace Umbraco.AI.BrowserAI.Models;

/// <summary>
/// Represents a job to be processed by browser-based AI.
/// </summary>
public class BrowserAIJob
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The full prompt text to process.
    /// </summary>
    public string Prompt { get; set; } = string.Empty;

    /// <summary>
    /// The type of operation (e.g., "chat", "summarize", "translate").
    /// </summary>
    public string OperationType { get; set; } = "chat";

    /// <summary>
    /// Current status of the job.
    /// </summary>
    public BrowserAIJobStatus Status { get; set; } = BrowserAIJobStatus.Pending;

    /// <summary>
    /// The result text, populated when job completes successfully.
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Error message, populated when job fails.
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// When the job was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// When the job completed (successfully or with failure).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
}
