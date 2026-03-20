using Microsoft.Extensions.AI;
using Umbraco.AI.Core.Models;
using Umbraco.AI.Core.Providers;

namespace Umbraco.AI.BrowserAI;

/// <summary>
/// AI chat capability for Browser AI provider.
/// </summary>
public class BrowserAIChatCapability(BrowserAIProvider provider) : AIChatCapabilityBase<BrowserAIProviderSettings>(provider)
{
    private const string ChatModel = "gemini-nano";
    private const string SummarizeModel = "gemini-nano-summarize";
    private const string TranslateModel = "gemini-nano-translate";

    private new BrowserAIProvider Provider => (BrowserAIProvider)base.Provider;

    /// <inheritdoc />
    protected override Task<IReadOnlyList<AIModelDescriptor>> GetModelsAsync(
        BrowserAIProviderSettings settings,
        CancellationToken cancellationToken = default)
    {
        var models = new List<AIModelDescriptor>
        {
            new(new AIModelRef(Provider.Id, ChatModel), "Gemini Nano (Chat)"),
            new(new AIModelRef(Provider.Id, SummarizeModel), "Gemini Nano (Summarize)"),
            new(new AIModelRef(Provider.Id, TranslateModel), "Gemini Nano (Translate)")
        };

        return Task.FromResult<IReadOnlyList<AIModelDescriptor>>(models);
    }

    /// <inheritdoc />
    protected override IChatClient CreateClient(BrowserAIProviderSettings settings, string? modelId)
    {
        var operationType = modelId switch
        {
            SummarizeModel => "summarize",
            TranslateModel => "translate",
            _ => "chat"
        };

        return new BrowserAIChatClient(
            Provider.JobStore,
            Provider.Logger,
            settings,
            operationType);
    }
}
