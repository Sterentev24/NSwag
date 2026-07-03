import { NextRequest, NextResponse } from 'next/server';
import { readDb, stripPasswordHash } from '@/lib/db';
import { comparePassword } from '@/lib/password';
import { signToken } from '@/lib/auth';

export async function POST(req: NextRequest): Promise<NextResponse> {
    let body: { username?: string; password?: string };
    try {
        body = await req.json();
    } catch {
        return NextResponse.json({ error: 'Invalid JSON body' }, { status: 400 });
    }

    if (!body.username || !body.password) {
        return NextResponse.json({ error: 'username and password are required' }, { status: 400 });
    }

    const db = readDb();
    const user = db.users.find((u) => u.username === body.username);
    if (!user) {
        return NextResponse.json({ error: 'Invalid credentials' }, { status: 401 });
    }

    const ok = await comparePassword(body.password, user.passwordHash);
    if (!ok) {
        return NextResponse.json({ error: 'Invalid credentials' }, { status: 401 });
    }

    const token = signToken({ sub: user.id, username: user.username, role: user.role });
    return NextResponse.json({ token, user: stripPasswordHash(user) }, { status: 200 });
}
