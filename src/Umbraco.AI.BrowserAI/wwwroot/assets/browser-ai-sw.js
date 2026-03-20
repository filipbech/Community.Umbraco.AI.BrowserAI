/**
 * Browser AI Service Worker
 *
 * This service worker polls for Browser AI jobs and processes them using
 * Chrome's built-in Prompt API (LanguageModel).
 *
 * Known limitations:
 * - Requires Chrome with Prompt API enabled
 * - No function calling / tool use support in current Chrome AI APIs
 * - Background Sync is not guaranteed to fire on a precise schedule; latency may vary 5–60s when tab is closed
 */

const POLL_ENDPOINT = '/umbraco/api/browserai/jobs/next';
const RESULT_ENDPOINT = (id) => `/umbraco/api/browserai/jobs/${id}/result`;
const ERROR_ENDPOINT = (id) => `/umbraco/api/browserai/jobs/${id}/error`;
const SYNC_TAG = 'browser-ai-poll';

// Wake up via Background Sync
self.addEventListener('sync', (event) => {
    if (event.tag === SYNC_TAG) {
        event.waitUntil(processNextJob());
    }
});

// Also handle periodic sync if browser supports it
self.addEventListener('periodicsync', (event) => {
    if (event.tag === SYNC_TAG) {
        event.waitUntil(processNextJob());
    }
});

// Handle messages from the main page (fallback polling)
self.addEventListener('message', (event) => {
    if (event.data?.type === 'POLL') {
        event.waitUntil(processNextJob());
    }
});

/**
 * Check if the Prompt API (LanguageModel) is available.
 */
async function isLanguageModelAvailable() {
    // Check for the new LanguageModel API
    if (typeof LanguageModel !== 'undefined') {
        try {
            const availability = await LanguageModel.availability();
            return availability === 'available' || availability === 'downloadable';
        } catch (e) {
            console.warn('[BrowserAI SW] Error checking LanguageModel availability:', e);
        }
    }

    // Fallback: check for legacy self.ai API
    if (self.ai?.languageModel) {
        try {
            const availability = await self.ai.languageModel.availability();
            return availability === 'available' || availability === 'downloadable';
        } catch (e) {
            console.warn('[BrowserAI SW] Error checking self.ai availability:', e);
        }
    }

    return false;
}

/**
 * Create a language model session.
 */
async function createSession(systemPrompt) {
    const initialPrompts = systemPrompt
        ? [{ role: 'system', content: systemPrompt }]
        : undefined;

    // Try new LanguageModel API first
    if (typeof LanguageModel !== 'undefined') {
        return await LanguageModel.create({ initialPrompts });
    }

    // Fallback to legacy self.ai API
    if (self.ai?.languageModel) {
        return await self.ai.languageModel.create({
            systemPrompt: systemPrompt || 'You are a helpful assistant.'
        });
    }

    throw new Error('No language model API available');
}

/**
 * Process the next pending job from the queue.
 */
async function processNextJob() {
    // Check browser AI availability
    const available = await isLanguageModelAvailable();
    if (!available) {
        console.warn('[BrowserAI SW] Language Model not available in this browser');
        return;
    }

    // Fetch next pending job
    let response;
    try {
        response = await fetch(POLL_ENDPOINT, {
            credentials: 'include', // sends Umbraco auth cookies
        });
    } catch (e) {
        console.warn('[BrowserAI SW] Error fetching next job:', e);
        return;
    }

    if (response.status === 204) {
        // Empty queue
        return;
    }

    if (!response.ok) {
        console.warn('[BrowserAI SW] Error response from server:', response.status);
        return;
    }

    let job;
    try {
        job = await response.json();
    } catch (e) {
        console.warn('[BrowserAI SW] Error parsing job response:', e);
        return;
    }

    console.log('[BrowserAI SW] Processing job:', job.id, job.operationType);

    try {
        let result;

        // Use the Prompt API for all operations
        const session = await createSession('You are a helpful assistant.');

        if (job.operationType === 'summarize') {
            result = await session.prompt(`Please summarize the following text concisely:\n\n${job.prompt}`);
        } else if (job.operationType === 'translate') {
            result = await session.prompt(`Please translate the following text to English:\n\n${job.prompt}`);
        } else {
            // Default: direct prompt
            result = await session.prompt(job.prompt);
        }

        // Post result back
        await fetch(RESULT_ENDPOINT(job.id), {
            method: 'POST',
            credentials: 'include',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ result }),
        });

        console.log('[BrowserAI SW] Job completed:', job.id);

        // Immediately check for more work
        await processNextJob();

    } catch (err) {
        console.error('[BrowserAI SW] Error processing job:', job.id, err);

        try {
            await fetch(ERROR_ENDPOINT(job.id), {
                method: 'POST',
                credentials: 'include',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ error: err.message || 'Unknown error' }),
            });
        } catch (postError) {
            console.error('[BrowserAI SW] Error posting error response:', postError);
        }
    }
}
