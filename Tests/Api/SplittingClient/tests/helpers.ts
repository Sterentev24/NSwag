import { beforeEach, afterEach } from 'vitest';
import { mkdtempSync, rmSync, existsSync, copyFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';

const SEED_PATH = join(process.cwd(), 'db.seed.json');

let currentDir: string | null = null;

/**
 * Point the API at a fresh copy of db.seed.json in a temp dir before each test,
 * and clean it up afterwards. Uses env DB_PATH which src/lib/db.ts respects.
 */
export function useIsolatedDb() {
    beforeEach(() => {
        currentDir = mkdtempSync(join(tmpdir(), 'splitting-api-'));
        const target = join(currentDir, 'db.json');
        if (existsSync(SEED_PATH)) copyFileSync(SEED_PATH, target);
        process.env.DB_PATH = target;
    });
    afterEach(() => {
        if (currentDir && existsSync(currentDir)) rmSync(currentDir, { recursive: true, force: true });
        currentDir = null;
        delete process.env.DB_PATH;
    });
}

/**
 * Build a minimal NextRequest-compatible object. The route handlers we test only
 * read `.json()` and `.headers`, so full Next fetch stubs are unnecessary.
 */
export function mockRequest(init: { body?: unknown; token?: string; headers?: Record<string, string> } = {}) {
    const headers = new Headers(init.headers ?? {});
    if (init.token) headers.set('Authorization', `Bearer ${init.token}`);
    const body = init.body === undefined ? undefined : JSON.stringify(init.body);
    return {
        json: async () => (init.body === undefined ? {} : init.body),
        headers,
    } as unknown as import('next/server').NextRequest;
}
