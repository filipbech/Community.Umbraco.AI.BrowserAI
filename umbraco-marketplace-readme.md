# Browser AI Provider

An experimental community AI provider for Umbraco that runs AI inference locally in the browser using Chrome's built-in Gemini Nano model.

## Features

- Chat completions using Chrome's Prompt API (Gemini Nano)
- Text summarization and translation
- Fully local — no API keys, no cloud costs, no data leaves the machine
- Integration with Umbraco's AI middleware pipeline

## Requirements

- Umbraco 15+
- Umbraco.AI.Core 1.6+
- Chrome 127+ on desktop with Prompt API enabled

## Getting Started

1. Install the package: `dotnet add package Community.Umbraco.AI.BrowserAI`
2. Register the services: `services.AddBrowserAI();`
3. Enable Chrome's Prompt API (see package README for Chrome setup steps)
4. Open the Umbraco backoffice in Chrome — the provider activates automatically
