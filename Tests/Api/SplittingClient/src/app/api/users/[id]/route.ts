import { NextRequest, NextResponse } from 'next/server';
import { readDb, writeDb, stripPasswordHash } from '@/lib/db';
import { requireAuth } from '@/lib/auth';

type Ctx = { params: Promise<{ id: string }> };

export async function GET(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    const db = readDb();
    const user = db.users.find((u) => u.id === id);
    if (!user) return NextResponse.json({ error: 'User not found' }, { status: 404 });

    return NextResponse.json(stripPasswordHash(user), { status: 200 });
}

export async function PUT(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    let body: { username?: string; email?: string; role?: 'admin' | 'user' };
    try {
        body = await req.json();
    } catch {
        return NextResponse.json({ error: 'Invalid JSON body' }, { status: 400 });
    }

    const db = readDb();
    const idx = db.users.findIndex((u) => u.id === id);
    if (idx < 0) return NextResponse.json({ error: 'User not found' }, { status: 404 });

    const current = db.users[idx];
    db.users[idx] = {
        ...current,
        username: body.username ?? current.username,
        email: body.email ?? current.email,
        role: body.role ?? current.role,
    };
    writeDb(db);

    return NextResponse.json(stripPasswordHash(db.users[idx]), { status: 200 });
}

export async function DELETE(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    const db = readDb();
    const before = db.users.length;
    db.users = db.users.filter((u) => u.id !== id);
    if (db.users.length === before) {
        return NextResponse.json({ error: 'User not found' }, { status: 404 });
    }
    writeDb(db);

    return new NextResponse(null, { status: 204 });
}
