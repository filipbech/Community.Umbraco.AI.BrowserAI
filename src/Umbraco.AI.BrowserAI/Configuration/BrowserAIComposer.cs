using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace Community.Umbraco.AI.BrowserAI.Configuration;

/// <summary>
/// Composer for the BrowserAI package, responsible for registering the necessary services with the Umbraco dependency injection container.
/// </summary>
public class BrowserAIComposer : IComposer
{
    /// <summary>
    /// Registers the BrowserAI services with the Umbraco dependency injection container.
    /// </summary>
    /// <param name="builder"></param>
    public void Compose(IUmbracoBuilder builder)
    {
        builder.Services.AddBrowserAI();
    }
}