import { defineConfig, devices } from '@playwright/test';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const apiDir = resolve(here, '../../Api/SplittingClient');
const uiDir = resolve(here, '../../UI/SplittingClient');

export default defineConfig({
    testDir: './tests',
    fullyParallel: false,
    workers: 1,
    reporter: 'list',
    globalSetup: fileURLToPath(new URL('./global-setup.ts', import.meta.url)),
    timeout: 60_000,
    expect: { timeout: 15_000 },
    use: {
        baseURL: 'http://localhost:5173',
        trace: 'on-first-retry',
        navigationTimeout: 30_000,
        actionTimeout: 15_000,
    },
    webServer: [
        {
            command: 'npm run dev',
            cwd: apiDir,
            port: 3001,
            reuseExistingServer: false,
            timeout: 60_000,
        },
        {
            command: 'npm run dev',
            cwd: uiDir,
            port: 5173,
            reuseExistingServer: false,
            timeout: 60_000,
        },
    ],
    projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
});
