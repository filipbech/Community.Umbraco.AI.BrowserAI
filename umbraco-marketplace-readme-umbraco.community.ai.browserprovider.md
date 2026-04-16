# Browser AI Provider

An experimental community AI provider for Umbraco that runs AI inference entirely in the browser using Chrome's built-in Gemini Nano model. No API keys needed, no cloud costs, and no data leaves the machine.

> **Note:** This package is experimental. Chrome's Prompt API is an early-stage feature and may change between browser updates.

## Features

- Chat completions, summarization, and translation using Chrome's Prompt API
- Fully local inference — your data never leaves the browser
- Near-instant job delivery via Umbraco's built-in SignalR infrastructure
- Configurable timeout with optional fallback to another AI provider
- Zero configuration needed beyond enabling Chrome flags

## Requirements

- Umbraco 15+ with Umbraco.AI.Core 1.6+
- Chrome 127+ on desktop with the Prompt API enabled
- ~22 GB free disk space for the Gemini Nano model download
- The Umbraco backoffice must be open in Chrome for the provider to work

## Getting Started

1. Install the package: `dotnet add package Umbraco.Community.AI.BrowserProvider`
2. Register the services in `Program.cs`: `services.AddBrowserAI();`
3. Enable Chrome's Prompt API:
   - Set `chrome://flags/#prompt-api-for-gemini-nano` to **Enabled**
   - Set `chrome://flags/#optimization-guide-on-device-model` to **Enabled BypassPerfRequirement**
   - Restart Chrome
   - Go to `chrome://components` and update **Optimization Guide On Device Model**
4. Open the Umbraco backoffice in Chrome — the provider activates automatically

## How It Works

When an AI request is made, the provider queues a job on the server. A SignalR notification is pushed to the backoffice, where a script picks up the job and processes it using Chrome's local Gemini Nano model. The result is posted back to the server and returned to the caller.

For full documentation and configuration options, see the [GitHub repository](https://github.com/filipbech/Umbraco.Community.AI.BrowserProvider).
