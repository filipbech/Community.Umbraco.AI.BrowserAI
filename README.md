# Browser AI Provider

An experimental community AI provider for Umbraco that routes inference requests to Chrome's built-in Gemini Nano model. The browser runs the model entirely locally — no API keys needed, no data leaves the machine, and no cloud costs.

> **Note:** This is an experimental package. Chrome's Prompt API is still an early-stage feature and may change or break between browser updates. Use at your own risk.

## How It Works

1. When an AI request is made, the provider creates a job in an in-memory queue on the server
2. A script running in the Umbraco backoffice polls for pending jobs
3. The script processes jobs using Chrome's Prompt API (`LanguageModel`)
4. Results are posted back to the server and returned to the caller

The Umbraco backoffice must be open in a supported Chrome browser for the provider to function.

## Chrome Setup

Chrome's Prompt API and Gemini Nano model require manual activation. Follow these steps:

### 1. Update Chrome

Make sure you are running **Chrome 127 or later** on desktop (Windows, macOS, or Linux). The Prompt API is not available on mobile.

### 2. Enable the Prompt API flag

1. Open `chrome://flags/#prompt-api-for-gemini-nano` in Chrome
2. Set the flag to **Enabled**

### 3. Enable the on-device model

1. Open `chrome://flags/#optimization-guide-on-device-model`
2. Set the flag to **Enabled BypassPerfRequirement**

### 4. Restart Chrome

Close and reopen Chrome completely for the flags to take effect.

### 5. Download the Gemini Nano model

1. Open `chrome://components`
2. Find **Optimization Guide On Device Model**
3. Click **Check for update** and wait for the download to complete (the model is approximately 2 GB, but Chrome requires around 22 GB of free disk space)

### 6. Verify

Open Chrome DevTools (F12) and run in the console:

```js
await LanguageModel.availability();
```

If it returns `"available"`, you're good to go. If it returns `"downloadable"` or `"downloading"`, the model is still being fetched — wait and try again.

## Installation

```bash
dotnet add package Community.Umbraco.AI.BrowserAI
```

Register the services in your `Program.cs`:

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
| `TimeoutSeconds` | `30` | How long to wait for a browser response before timing out |
| `MaxJobAgeSeconds` | `300` | How long to keep jobs before purging them |
| `FallbackProviderId` | `null` | Optional provider ID to fall back to when Browser AI times out (e.g., `openai`, `anthropic`) |

## Supported Operations

- **Chat** — General chat completions via `LanguageModel`
- **Summarize** — Text summarization
- **Translate** — Translation to English

## Known Limitations

- Chrome-only — requires Chrome 127+ on desktop
- The Prompt API is experimental and may change between Chrome versions
- No function calling / tool use support
- Latency can vary from 5–60 seconds depending on prompt length and hardware
- Job store is in-memory — restarting Umbraco discards pending jobs
- The Umbraco backoffice must be open in a supported browser for the provider to work

## API Endpoints

| Endpoint | Method | Auth | Description |
|----------|--------|------|-------------|
| `/umbraco/api/browserai/status` | GET | None | Health check |
| `/umbraco/api/browserai/jobs/next` | GET | Required | Get next pending job |
| `/umbraco/api/browserai/jobs/{id}/result` | POST | Required | Post job result |
| `/umbraco/api/browserai/jobs/{id}/error` | POST | Required | Post job error |

## License

See [LICENSE.md](LICENSE.md) for details.
