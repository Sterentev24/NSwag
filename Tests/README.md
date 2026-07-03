# Tests — end-to-end demo for the SplitByDto TypeScript client

This folder hosts a self-contained demo that exercises the `TypeScriptOutputMode.SplitByDto` mode
added by the parent PR. It contains a Next.js API, a Vite + React UI that consumes the split
NSwag-generated client, plus unit and Playwright end-to-end tests.

```
Tests/
├── Api/SplittingClient/     Next.js 15 API — Auth, Users, Orders, Goods (JWT, file DB)
├── UI/SplittingClient/      Vite + React + React Router (uses the split NSwag client)
└── e2e/SplittingClient/     Playwright scenarios that drive both apps
```

## Prerequisites

- Node.js 20 or newer, npm 10+.
- .NET 10 SDK (only required if you want to regenerate the client from `openapi.json`).
- The forked NSwag CLI built at least once:
  ```
  dotnet build src/NSwag.ConsoleCore/NSwag.ConsoleCore.csproj
  ```

## First-time install

Run once from the repo root:

```
cd Tests/Api/SplittingClient && npm install
cd ../../UI/SplittingClient    && npm install
cd ../../e2e/SplittingClient   && npm install
npx playwright install chromium
```

## Regenerating the split client (only if `openapi.json` changed)

```
cd Tests/UI/SplittingClient
npm run generate:client
```

This shells out to the forked `dotnet-nswag.exe` with
`--OutputMode:SplitByDto --OutputFolder:src/api`. If the CLI is missing the script points you
at the required `dotnet build` command.

## Running the demo

Two terminals — or a single Playwright run that starts both automatically.

**Manual dev:**

```
# terminal 1
cd Tests/Api/SplittingClient
npm run dev              # http://localhost:3001

# terminal 2
cd Tests/UI/SplittingClient
npm run dev              # http://localhost:5173
```

Open http://localhost:5173. Seeded users:

| Username | Password  | Role  |
|----------|-----------|-------|
| admin    | admin123  | admin |
| user1    | user123   | user  |

After login you land on `/orders`. The left sidebar (three-line hamburger toggle) has icons for
Orders, Goods and Users — each is its own CRUD page powered by the split client.

## Running the tests

**API unit tests (Vitest, no server needed):**

```
cd Tests/Api/SplittingClient
npm test
```

Each test runs against an isolated temp copy of `db.seed.json` via a `DB_PATH` env var, so
the tests never touch your local `db.json`.

**UI component tests (Vitest + Testing Library):**

```
cd Tests/UI/SplittingClient
npm test
```

Uses jsdom + a stubbed `window.fetch` so the tests do not need the API running.

**End-to-end (Playwright — auto-starts API and UI):**

```
cd Tests/e2e/SplittingClient
npm test
```

Playwright's `webServer` config starts both `Api/SplittingClient` and `UI/SplittingClient` on
their dev ports before any test runs, deletes `Api/SplittingClient/db.json` in `globalSetup`
(so each run starts from the pristine `db.seed.json`), and shuts them down when tests finish.

Ports used during E2E: **3001** (API) and **5173** (UI). Make sure they are free before
running.

## Regenerating the seed database

The seed contains bcrypt hashes so it can be committed safely. To rotate the demo credentials
edit `Tests/Api/SplittingClient/scripts/seed.mjs` and run:

```
cd Tests/Api/SplittingClient
npm run seed:generate
```

That overwrites `db.seed.json`. On the next API start (with no `db.json` present) the API
copies the seed to `db.json`.

## What this demo proves for the parent PR

1. `openapi.json` → `dotnet-nswag openapi2tsclient --OutputMode:SplitByDto` produces 24 files
   (4 clients + 18 DTO/enum + `shared.ts` + `index.ts`) that compile under `tsc --strict`.
2. Each client imports only the DTOs it references; each DTO imports only its transitive
   dependencies. `shared.ts` holds `ApiException` / `throwException` / `FileParameter` and is
   imported by every client.
3. The generated client works end-to-end against a real Next.js API (login → JWT → CRUD on
   three resources), verified by Playwright.
