import { NextResponse } from 'next/server';
import { readFileSync } from 'node:fs';
import { join } from 'node:path';

export async function GET(): Promise<NextResponse> {
    const path = join(process.cwd(), 'openapi.json');
    const spec = JSON.parse(readFileSync(path, 'utf8'));
    return NextResponse.json(spec, { status: 200 });
}
