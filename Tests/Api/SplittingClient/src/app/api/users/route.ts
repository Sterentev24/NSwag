import { NextRequest, NextResponse } from 'next/server';
import { readDb, writeDb, nextId, stripPasswordHash, StoredUser } from '@/lib/db';
import { requireAuth } from '@/lib/auth';
import { hashPassword } from '@/lib/password';

export async function GET(req: NextRequest): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const db = readDb();
    return NextResponse.json(db.users.map(stripPasswordHash), { status: 200 });
}

export async function POST(req: NextRequest): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    let body: {
        username?: string;
        password?: string;
        email?: string;
        role?: 'admin' | 'user';
    };
    try {
        body = await req.json();
    } catch {
        return NextResponse.json({ error: 'Invalid JSON body' }, { status: 400 });
    }

    if (!body.username || !body.password || !body.email || !body.role) {
        return NextResponse.json(
            { error: 'username, password, email, role are required' },
            { status: 400 }
        );
    }

    const db = readDb();
    if (db.users.some((u) => u.username === body.username)) {
        return NextResponse.json({ error: 'username already taken' }, { status: 409 });
    }

    const user: StoredUser = {
        id: 'u_' + nextId(),
        username: body.username,
        email: body.email,
        role: body.role,
        passwordHash: await hashPassword(body.password),
        createdAt: new Date().toISOString(),
    };
    db.users.push(user);
    writeDb(db);

    return NextResponse.json(stripPasswordHash(user), { status: 201 });
}
