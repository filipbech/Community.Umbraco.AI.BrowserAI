using Umbraco.AI.BrowserAI.Models;

namespace Umbraco.AI.BrowserAI;

/// <summary>
/// Store for managing Browser AI jobs.
/// </summary>
public interface IBrowserAIJobStore
{
    /// <summary>
    /// Creates a new job with the specified prompt and operation type.
    /// </summary>
    /// <param name="prompt">The prompt text.</param>
    /// <param name="operationType">The operation type (e.g., "chat", "summarize", "translate").</param>
    /// <returns>The created job.</returns>
    Task<BrowserAIJob> CreateJobAsync(string prompt, string operationType);

    /// <summary>
    /// Gets the next pending job and marks it as processing.
    /// </summary>
    /// <returns>The next pending job, or null if none available.</returns>
    Task<BrowserAIJob?> GetNextPendingJobAsync();

    /// <summary>
    /// Gets a job by its ID.
    /// </summary>
    /// <param name="id">The job ID.</param>
    /// <returns>The job, or null if not found.</returns>
    Task<BrowserAIJob?> GetJobAsync(string id);

    /// <summary>
    /// Marks a job as complete with the specified result.
    /// </summary>
    /// <param name="id">The job ID.</param>
    /// <param name="result">The result text.</param>
    Task MarkCompleteAsync(string id, string result);

    /// <summary>
    /// Marks a job as failed with the specified error.
    /// </summary>
    /// <param name="id">The job ID.</param>
    /// <param name="error">The error message.</param>
    Task MarkFailedAsync(string id, string error);

    /// <summary>
    /// Removes jobs older than the specified age.
    /// </summary>
    /// <param name="maxAge">Maximum age for jobs to keep.</param>
    Task PurgeExpiredJobsAsync(TimeSpan maxAge);
}
