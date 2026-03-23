using System.Collections.Concurrent;
using Community.Umbraco.AI.BrowserAI.Models;

namespace Community.Umbraco.AI.BrowserAI;

/// <summary>
/// In-memory implementation of <see cref="IBrowserAIJobStore"/>.
/// </summary>
/// <remarks>
/// Thread-safe implementation using ConcurrentDictionary.
/// Note: Jobs are lost when the application restarts.
/// </remarks>
public class InMemoryBrowserAIJobStore : IBrowserAIJobStore
{
    private readonly ConcurrentDictionary<string, BrowserAIJob> _jobs = new();
    private readonly object _pendingLock = new();

    /// <inheritdoc />
    public Task<BrowserAIJob> CreateJobAsync(string prompt, string operationType)
    {
        var job = new BrowserAIJob
        {
            Prompt = prompt,
            OperationType = operationType,
            Status = BrowserAIJobStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _jobs[job.Id] = job;

        return Task.FromResult(job);
    }

    /// <inheritdoc />
    public Task<BrowserAIJob?> GetNextPendingJobAsync()
    {
        lock (_pendingLock)
        {
            var pendingJob = _jobs.Values
                .Where(j => j.Status == BrowserAIJobStatus.Pending)
                .OrderBy(j => j.CreatedAt)
                .FirstOrDefault();

            if (pendingJob is not null)
            {
                pendingJob.Status = BrowserAIJobStatus.Processing;
            }

            return Task.FromResult(pendingJob);
        }
    }

    /// <inheritdoc />
    public Task<BrowserAIJob?> GetJobAsync(string id)
    {
        _jobs.TryGetValue(id, out var job);
        return Task.FromResult(job);
    }

    /// <inheritdoc />
    public Task MarkCompleteAsync(string id, string result)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            job.Status = BrowserAIJobStatus.Complete;
            job.Result = result;
            job.CompletedAt = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task MarkFailedAsync(string id, string error)
    {
        if (_jobs.TryGetValue(id, out var job))
        {
            job.Status = BrowserAIJobStatus.Failed;
            job.Error = error;
            job.CompletedAt = DateTimeOffset.UtcNow;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task PurgeExpiredJobsAsync(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow.Subtract(maxAge);
        var expiredIds = _jobs
            .Where(kvp => kvp.Value.CreatedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var id in expiredIds)
        {
            _jobs.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }
}
