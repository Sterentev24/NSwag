import { NextRequest, NextResponse } from 'next/server';
import { readDb, writeDb } from '@/lib/db';
import { requireAuth } from '@/lib/auth';

type Ctx = { params: Promise<{ id: string }> };

export async function GET(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    const db = readDb();
    const order = db.orders.find((o) => o.id === id);
    if (!order) return NextResponse.json({ error: 'Order not found' }, { status: 404 });

    return NextResponse.json(order, { status: 200 });
}

export async function PUT(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    let body: { status?: 'pending' | 'paid' | 'shipped' | 'cancelled' };
    try {
        body = await req.json();
    } catch {
        return NextResponse.json({ error: 'Invalid JSON body' }, { status: 400 });
    }

    const db = readDb();
    const idx = db.orders.findIndex((o) => o.id === id);
    if (idx < 0) return NextResponse.json({ error: 'Order not found' }, { status: 404 });

    if (body.status) db.orders[idx].status = body.status;
    writeDb(db);

    return NextResponse.json(db.orders[idx], { status: 200 });
}

export async function DELETE(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    const db = readDb();
    const before = db.orders.length;
    db.orders = db.orders.filter((o) => o.id !== id);
    if (db.orders.length === before) {
        return NextResponse.json({ error: 'Order not found' }, { status: 404 });
    }
    writeDb(db);

    return new NextResponse(null, { status: 204 });
}
