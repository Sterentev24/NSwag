import { NextRequest, NextResponse } from 'next/server';
import { readDb, writeDb } from '@/lib/db';
import { requireAuth } from '@/lib/auth';

type Ctx = { params: Promise<{ id: string }> };

export async function GET(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    const db = readDb();
    const good = db.goods.find((g) => g.id === id);
    if (!good) return NextResponse.json({ error: 'Good not found' }, { status: 404 });

    return NextResponse.json(good, { status: 200 });
}

export async function PUT(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    let body: { name?: string; description?: string; price?: number; stock?: number };
    try {
        body = await req.json();
    } catch {
        return NextResponse.json({ error: 'Invalid JSON body' }, { status: 400 });
    }

    const db = readDb();
    const idx = db.goods.findIndex((g) => g.id === id);
    if (idx < 0) return NextResponse.json({ error: 'Good not found' }, { status: 404 });

    const current = db.goods[idx];
    db.goods[idx] = {
        ...current,
        name: body.name ?? current.name,
        description: body.description ?? current.description,
        price: body.price ?? current.price,
        stock: body.stock ?? current.stock,
    };
    writeDb(db);

    return NextResponse.json(db.goods[idx], { status: 200 });
}

export async function DELETE(req: NextRequest, ctx: Ctx): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const { id } = await ctx.params;
    const db = readDb();
    const before = db.goods.length;
    db.goods = db.goods.filter((g) => g.id !== id);
    if (db.goods.length === before) {
        return NextResponse.json({ error: 'Good not found' }, { status: 404 });
    }
    writeDb(db);

    return new NextResponse(null, { status: 204 });
}
