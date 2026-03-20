/**
 * Browser AI Job Processor
 *
 * This script runs in the main page context and polls for Browser AI jobs,
 * processing them using Chrome's Prompt API (LanguageModel).
 *
 * Note: LanguageModel is only available in the main window context, not in Service Workers.
 */

const POLL_ENDPOINT = '/umbraco/api/browserai/jobs/next';
const RESULT_ENDPOINT = (id) => `/umbraco/api/browserai/jobs/${id}/result`;
const ERROR_ENDPOINT = (id) => `/umbraco/api/browserai/jobs/${id}/error`;
const POLL_INTERVAL = 1000; // 1 second - balance between responsiveness and server load
const MAX_PROMPT_LENGTH = 4000; // Limit prompt length to avoid issues

let isProcessing = false;
let modelReady = false;

(async function () {
    console.log('[BrowserAI] Initializing Browser AI job processor');

    // Check and report browser AI availability
    const available = await checkAndReportAvailability();

    if (!available) {
        console.warn('[BrowserAI] Language Model not available - job processing disabled');
        return;
    }

    // Wait for model to be fully ready and verify it works
    await waitForModelReady();

    // Start polling for jobs
    console.log('[BrowserAI] Starting job polling (every ' + POLL_INTERVAL + 'ms)');
    setInterval(processNextJob, POLL_INTERVAL);

    // Also process immediately
    await processNextJob();
})();

/**
 * Wait for the model to be fully ready and verify it works with a test prompt.
 */
async function waitForModelReady() {
    console.log('[BrowserAI] Waiting for model to be fully ready...');

    // Check availability until it's "available" (not just downloadable)
    let attempts = 0;
    const maxAttempts = 60; // Wait up to 60 seconds

    while (attempts < maxAttempts) {
        try {
            const availability = await LanguageModel.availability();
            console.log('[BrowserAI] Model availability check:', availability);

            if (availability === 'available') {
                // Model is ready, try a test prompt
                console.log('[BrowserAI] Model reports available, testing with simple prompt...');

                try {
                    const testSession = await LanguageModel.create();
                    console.log('[BrowserAI] Test session created');

                    const testResult = await testSession.prompt('Say "Hello" and nothing else.');
                    console.log('[BrowserAI] Test prompt succeeded! Result:', testResult);

                    modelReady = true;
                    updateStatusIndicator('active');
                    return;
                } catch (testErr) {
                    console.warn('[BrowserAI] Test prompt failed:', testErr.name, testErr.message);
                    // Continue waiting - model might not be fully ready
                }
            }

            if (availability === 'downloading') {
                updateStatusIndicator('downloading');
            }
        } catch (e) {
            console.warn('[BrowserAI] Error checking availability:', e);
        }

        attempts++;
        await new Promise(resolve => setTimeout(resolve, 1000));
    }

    console.warn('[BrowserAI] Model did not become ready after', maxAttempts, 'seconds');
    updateStatusIndicator('unavailable');
}

/**
 * Process the next pending job from the queue.
 */
async function processNextJob() {
    // Prevent concurrent processing and ensure model is ready
    if (isProcessing) return;
    if (!modelReady) {
        console.log('[BrowserAI] Model not ready yet, skipping job processing');
        return;
    }

    isProcessing = true;

    try {
        // Fetch next pending job
        const response = await fetch(POLL_ENDPOINT, {
            credentials: 'include',
        });

        if (response.status === 204) {
            // Empty queue
            return;
        }

        if (!response.ok) {
            console.warn('[BrowserAI] Error response from server:', response.status);
            return;
        }

        const job = await response.json();
        console.log('[BrowserAI] Processing job:', job.id, job.operationType);
        console.log('[BrowserAI] Prompt length:', job.prompt.length, 'chars');
        console.log('[BrowserAI] Prompt preview:', job.prompt.substring(0, 200) + (job.prompt.length > 200 ? '...' : ''));
        const startTime = performance.now();

        try {
            // Truncate prompt if too long
            let promptText = job.prompt;
            if (promptText.length > MAX_PROMPT_LENGTH) {
                console.warn('[BrowserAI] Prompt too long, truncating from', promptText.length, 'to', MAX_PROMPT_LENGTH);
                promptText = promptText.substring(0, MAX_PROMPT_LENGTH) + '...';
            }

            // Build the final prompt
            let finalPrompt;
            if (job.operationType === 'summarize') {
                finalPrompt = `Please summarize the following text concisely:\n\n${promptText}`;
            } else if (job.operationType === 'translate') {
                finalPrompt = `Please translate the following text to English:\n\n${promptText}`;
            } else {
                finalPrompt = promptText;
            }

            console.log('[BrowserAI] Final prompt length:', finalPrompt.length);

            // Create a fresh session for each job
            console.log('[BrowserAI] Creating language model session...');
            const session = await LanguageModel.create();
            console.log('[BrowserAI] Session created, tokensSoFar:', session.tokensSoFar, 'maxTokens:', session.maxTokens, 'tokensLeft:', session.tokensLeft);
            console.log('[BrowserAI] Sending prompt...');

            const result = await session.prompt(finalPrompt);

            console.log('[BrowserAI] Model inference took:', Math.round(performance.now() - startTime), 'ms');
            console.log('[BrowserAI] Result length:', result.length);
            console.log('[BrowserAI] Result preview:', result.substring(0, 200) + (result.length > 200 ? '...' : ''));

            // Post result back
            await fetch(RESULT_ENDPOINT(job.id), {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ result }),
            });

            console.log('[BrowserAI] Job completed:', job.id);

            // Immediately check for more work
            isProcessing = false;
            await processNextJob();

        } catch (err) {
            console.error('[BrowserAI] Error processing job:', job.id);
            console.error('[BrowserAI] Error name:', err.name);
            console.error('[BrowserAI] Error message:', err.message);
            console.error('[BrowserAI] Full error:', err);

            await fetch(ERROR_ENDPOINT(job.id), {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ error: `${err.name}: ${err.message}` }),
            });
        }
    } catch (e) {
        console.warn('[BrowserAI] Error in job processing loop:', e);
    } finally {
        isProcessing = false;
    }
}

/**
 * Check browser AI availability and log status.
 * @returns {Promise<boolean>} True if Language Model API exists.
 */
async function checkAndReportAvailability() {
    // Check for LanguageModel API
    if (typeof LanguageModel !== 'undefined') {
        try {
            const availability = await LanguageModel.availability();
            console.log('[BrowserAI] LanguageModel API found, initial availability:', availability);

            if (availability === 'available' || availability === 'downloadable' || availability === 'downloading') {
                // API exists and model is either ready or will be ready
                return true;
            } else {
                // Status is "no", "unavailable", or something else
                console.warn('[BrowserAI] LanguageModel reports:', availability);
                console.info('[BrowserAI] To enable Gemini Nano, follow these steps:');
                console.info('[BrowserAI] 1. Go to: chrome://flags/#optimization-guide-on-device-model');
                console.info('[BrowserAI]    Set to "Enabled BypassPerfRequirement"');
                console.info('[BrowserAI] 2. Go to: chrome://flags/#prompt-api-for-gemini-nano');
                console.info('[BrowserAI]    Set to "Enabled"');
                console.info('[BrowserAI] 3. Restart Chrome');
                console.info('[BrowserAI] 4. Go to: chrome://components');
                console.info('[BrowserAI]    Find "Optimization Guide On Device Model"');
                console.info('[BrowserAI]    Click "Check for update" to download the model');
            }
        } catch (e) {
            console.warn('[BrowserAI] Error checking LanguageModel availability:', e);
        }
    } else {
        console.warn('[BrowserAI] LanguageModel API not found');
        console.info('[BrowserAI] Enable at: chrome://flags/#prompt-api-for-gemini-nano');
    }

    updateStatusIndicator('unavailable');
    return false;
}

/**
 * Update status indicator if it exists in the DOM.
 */
function updateStatusIndicator(status) {
    // Dispatch a custom event that the backoffice can listen to
    window.dispatchEvent(new CustomEvent('browser-ai-status', {
        detail: { status }
    }));
}
