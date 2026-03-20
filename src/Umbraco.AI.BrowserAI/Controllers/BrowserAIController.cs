using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Umbraco.AI.BrowserAI.Models;
using Umbraco.Cms.Web.Common.Authorization;

namespace Umbraco.AI.BrowserAI.Controllers;

/// <summary>
/// API controller for Browser AI job management.
/// </summary>
[ApiController]
[Route("umbraco/api/browserai")]
public class BrowserAIController : ControllerBase
{
    private readonly IBrowserAIJobStore _jobStore;
    private readonly IOptions<BrowserAIProviderSettings> _settings;
    private readonly ILogger<BrowserAIController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserAIController"/> class.
    /// </summary>
    public BrowserAIController(
        IBrowserAIJobStore jobStore,
        IOptions<BrowserAIProviderSettings> settings,
        ILogger<BrowserAIController> logger)
    {
        _jobStore = jobStore;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Health check endpoint - no auth required.
    /// </summary>
    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult GetStatus()
    {
        if (!_settings.Value.Enabled)
        {
            return StatusCode(503, new { error = "Browser AI provider is disabled" });
        }

        return Ok(new StatusResponse
        {
            Available = true,
            Version = "1.0"
        });
    }

    /// <summary>
    /// Gets the next pending job for processing.
    /// </summary>
    [HttpGet("jobs/next")]
    [AllowAnonymous] // TODO: Add proper auth - jobs are only useful from backoffice context
    public async Task<IActionResult> GetNextJob()
    {
        if (!_settings.Value.Enabled)
        {
            return StatusCode(503, new { error = "Browser AI provider is disabled" });
        }

        var job = await _jobStore.GetNextPendingJobAsync();

        if (job is null)
        {
            return NoContent();
        }

        _logger.LogDebug("Job {JobId} picked up by browser", job.Id);

        return Ok(new JobResponse
        {
            Id = job.Id,
            Prompt = job.Prompt,
            OperationType = job.OperationType
        });
    }

    /// <summary>
    /// Posts the result of a completed job.
    /// </summary>
    [HttpPost("jobs/{id}/result")]
    [AllowAnonymous] // TODO: Add proper auth
    public async Task<IActionResult> PostResult(string id, [FromBody] JobResultRequest request)
    {
        if (!_settings.Value.Enabled)
        {
            return StatusCode(503, new { error = "Browser AI provider is disabled" });
        }

        var job = await _jobStore.GetJobAsync(id);
        if (job is null)
        {
            return NotFound(new { error = "Job not found" });
        }

        await _jobStore.MarkCompleteAsync(id, request.Result);
        _logger.LogDebug("Job {JobId} completed successfully", id);

        return Ok();
    }

    /// <summary>
    /// Posts an error for a failed job.
    /// </summary>
    [HttpPost("jobs/{id}/error")]
    [AllowAnonymous] // TODO: Add proper auth
    public async Task<IActionResult> PostError(string id, [FromBody] JobErrorRequest request)
    {
        if (!_settings.Value.Enabled)
        {
            return StatusCode(503, new { error = "Browser AI provider is disabled" });
        }

        var job = await _jobStore.GetJobAsync(id);
        if (job is null)
        {
            return NotFound(new { error = "Job not found" });
        }

        await _jobStore.MarkFailedAsync(id, request.Error);
        _logger.LogWarning("Job {JobId} failed: {Error}", id, request.Error);

        return Ok();
    }
}
