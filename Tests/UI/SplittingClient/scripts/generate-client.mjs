// Regenerate the SplitByDto TypeScript client from Api/SplittingClient/openapi.json.
// Requires the forked NSwag CLI to be built at ../../../../artifacts/bin/NSwag.ConsoleCore/debug_net8.0.

import { spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const here = dirname(fileURLToPath(import.meta.url));
const uiRoot = resolve(here, '..');
const repoRoot = resolve(uiRoot, '..', '..', '..');
const cli = resolve(repoRoot, 'artifacts/bin/NSwag.ConsoleCore/debug_net8.0/dotnet-nswag.exe');
const input = resolve(repoRoot, 'Tests/Api/SplittingClient/openapi.json');
const output = resolve(uiRoot, 'src/api');

if (!existsSync(cli)) {
    console.error(`nswag CLI not found at ${cli}`);
    console.error(`Build the forked NSwag first:  dotnet build src/NSwag.ConsoleCore/NSwag.ConsoleCore.csproj`);
    process.exit(1);
}
if (!existsSync(input)) {
    console.error(`openapi.json not found at ${input}`);
    process.exit(1);
}

const args = [
    'openapi2tsclient',
    `/input:${input}`,
    `/outputFolder:${output}`,
    '/outputMode:SplitByDto',
    '/template:Fetch',
    '/typeStyle:Interface',
    '/operationGenerationMode:MultipleClientsFromFirstTagAndOperationName',
];

const result = spawnSync(cli, args, { stdio: 'inherit' });
process.exit(result.status ?? 1);
