import { existsSync, rmSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

// Deletes API/db.json before the run so the API re-seeds from db.seed.json on first read.
// Also warms up the API and UI dev servers because Next.js and Vite lazy-compile routes on
// first request — without warm-up the first E2E test can time out waiting for the response.
export default async function globalSetup() {
    const here = dirname(fileURLToPath(import.meta.url));
    const apiDbPath = resolve(here, '../../Api/SplittingClient/db.json');
    if (existsSync(apiDbPath)) {
        rmSync(apiDbPath);
    }

    // Best-effort warm-up. Errors are swallowed — the actual tests will still surface real problems.
    async function warm(url: string) {
        for (let i = 0; i < 30; i++) {
            try {
                const res = await fetch(url);
                if (res.status < 500) return;
            } catch {
                /* not ready yet */
            }
            await new Promise((r) => setTimeout(r, 1000));
        }
    }

    await Promise.all([
        warm('http://localhost:3001/api/openapi'),
        warm('http://localhost:5173/login'),
    ]);
}
