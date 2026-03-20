# Umbraco.AI.BrowserAI

Browser AI provider for Umbraco AI that routes inference requests to Chrome's built-in browser AI (Gemini Nano via `self.ai`). The browser runs the model locally; the server acts as a job queue.

## How It Works

1. When an AI request is made, the provider creates a job in an in-memory queue
2. A Service Worker running in the Umbraco backoffice polls for pending jobs
3. The Service Worker processes jobs using Chrome's `self.ai` API (Gemini Nano)
4. Results are posted back to the server and returned to the caller

## Requirements

- Chrome 127+ on desktop
- 22 GB free storage for Gemini Nano model
- Umbraco backoffice must be open in a supported browser

## Installation

```bash
dotnet add package Umbraco.AI.BrowserAI
```

Register the services in your `Startup.cs` or `Program.cs`:

```csharp
services.AddBrowserAI();
```

## Configuration

Add the following to your `appsettings.json`:

```json
{
  "Umbraco": {
    "AI": {
      "BrowserProvider": {
        "Enabled": true,
        "TimeoutSeconds": 30,
        "MaxJobAgeSeconds": 300,
        "FallbackProviderId": "openai"
      }
    }
  }
}
```

### Settings

| Setting | Default | Description |
|---------|---------|-------------|
| `Enabled` | `true` | Whether the Browser AI provider is enabled |
| `TimeoutSeconds` | `30` | How long to wait for a browser response |
| `MaxJobAgeSeconds` | `300` | How long to keep jobs before purging |
| `FallbackProviderId` | `null` | Optional provider to use when Browser AI times out |

## Supported Operations

- **Chat** - General chat completions using `self.ai.languageModel`
- **Summarize** - Text summarization using `self.ai.summarizer`
- **Translate** - Translation using `self.ai.translator`

## Known Limitations

- `self.ai` is Chrome-only; requires Chrome 127+ on desktop
- No function calling / tool use support in current Chrome AI APIs
- Background Sync latency may vary 5–60 seconds when tab is closed
- Job store is in-memory — restarting Umbraco discards pending jobs

## API Endpoints

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/umbraco/api/browserai/status` | GET | None | Health check |
| `/umbraco/api/browserai/jobs/next` | GET | Required | Get next pending job |
| `/umbraco/api/browserai/jobs/{id}/result` | POST | Required | Post job result |
| `/umbraco/api/browserai/jobs/{id}/error` | POST | Required | Post job error |
