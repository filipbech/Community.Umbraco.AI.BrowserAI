using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Community.Umbraco.AI.BrowserAI.Models;

namespace Community.Umbraco.AI.BrowserAI;

/// <summary>
/// Chat client that uses the browser-based job queue for AI processing.
/// </summary>
public class BrowserAIChatClient : IChatClient
{
    private readonly IBrowserAIJobStore _jobStore;
    private readonly ILogger _logger;
    private readonly BrowserAIProviderSettings _settings;
    private readonly string _operationType;
    private readonly IChatClient? _fallbackClient;

    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Initializes a new instance of the <see cref="BrowserAIChatClient"/> class.
    /// </summary>
    public BrowserAIChatClient(
        IBrowserAIJobStore jobStore,
        ILogger logger,
        BrowserAIProviderSettings settings,
        string operationType,
        IChatClient? fallbackClient = null)
    {
        _jobStore = jobStore;
        _logger = logger;
        _settings = settings;
        _operationType = operationType;
        _fallbackClient = fallbackClient;
    }

    /// <inheritdoc />
    public ChatClientMetadata Metadata => new("BrowserAI", new Uri("https://localhost"), "gemini-nano");

    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var prompt = BuildPromptFromMessages(messagesList);

        try
        {
            var result = await ProcessJobAsync(prompt, cancellationToken);
            return new ChatResponse(new ChatMessage(ChatRole.Assistant, result));
        }
        catch (TimeoutException) when (_fallbackClient is not null)
        {
            _logger.LogWarning("Browser AI timed out, falling back to {FallbackProvider}", _settings.FallbackProviderId);
            return await _fallbackClient.GetResponseAsync(chatMessages, options, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        var prompt = BuildPromptFromMessages(messagesList);

        string? result = null;
        bool useFallback = false;

        try
        {
            result = await ProcessJobAsync(prompt, cancellationToken);
        }
        catch (TimeoutException) when (_fallbackClient is not null)
        {
            _logger.LogWarning("Browser AI timed out, falling back to {FallbackProvider}", _settings.FallbackProviderId);
            useFallback = true;
        }

        if (useFallback && _fallbackClient is not null)
        {
            await foreach (var update in _fallbackClient.GetStreamingResponseAsync(chatMessages, options, cancellationToken))
            {
                yield return update;
            }
            yield break;
        }

        // Browser AI doesn't support streaming, so we return the complete result as a single update
        yield return new ChatResponseUpdate(
            role: ChatRole.Assistant,
            content: result ?? string.Empty);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => serviceType == typeof(IChatClient) ? this : null;

    /// <inheritdoc />
    public void Dispose()
    {
        // Nothing to dispose
    }

    private static string BuildPromptFromMessages(IList<ChatMessage> chatMessages)
    {
        // Combine all messages into a single prompt
        // The last user message is the primary prompt
        var userMessages = chatMessages
            .Where(m => m.Role == ChatRole.User)
            .ToList();

        if (userMessages.Count == 0)
        {
            return string.Empty;
        }

        // If there's only one user message, use it directly
        if (userMessages.Count == 1)
        {
            return userMessages[0].Text ?? string.Empty;
        }

        // Otherwise, build a conversation context
        return string.Join("\n\n", chatMessages.Select(m =>
            $"{m.Role}: {m.Text}"));
    }

    private async Task<string> ProcessJobAsync(string prompt, CancellationToken cancellationToken)
    {
        var job = await _jobStore.CreateJobAsync(prompt, _operationType);
        _logger.LogDebug("Browser AI job {JobId} created with operation type {OperationType}", job.Id, _operationType);

        var timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
        var deadline = DateTimeOffset.UtcNow.Add(timeout);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await Task.Delay(PollInterval, cancellationToken);
            var updated = await _jobStore.GetJobAsync(job.Id);

            if (updated?.Status == BrowserAIJobStatus.Complete)
            {
                _logger.LogDebug("Browser AI job {JobId} completed successfully", job.Id);
                return updated.Result ?? string.Empty;
            }

            if (updated?.Status == BrowserAIJobStatus.Failed)
            {
                _logger.LogWarning("Browser AI job {JobId} failed: {Error}", job.Id, updated.Error);
                throw new InvalidOperationException(updated.Error ?? "Browser AI failed");
            }
        }

        // Timeout reached
        await _jobStore.MarkFailedAsync(job.Id, "Timed out waiting for browser");
        _logger.LogWarning("Browser AI job {JobId} timed out after {Timeout} seconds", job.Id, _settings.TimeoutSeconds);

        throw new TimeoutException("Browser AI timed out. Is the backoffice open in a browser with Gemini Nano support?");
    }
}
