import { NextRequest, NextResponse } from 'next/server';
import { readDb, writeDb, nextId, StoredGood } from '@/lib/db';
import { requireAuth } from '@/lib/auth';

export async function GET(req: NextRequest): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const db = readDb();
    return NextResponse.json(db.goods, { status: 200 });
}

export async function POST(req: NextRequest): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    let body: {
        name?: string;
        description?: string;
        price?: number;
        stock?: number;
    };
    try {
        body = await req.json();
    } catch {
        return NextResponse.json({ error: 'Invalid JSON body' }, { status: 400 });
    }

    if (
        !body.name ||
        body.description === undefined ||
        typeof body.price !== 'number' ||
        typeof body.stock !== 'number'
    ) {
        return NextResponse.json(
            { error: 'name, description, price, stock are required' },
            { status: 400 }
        );
    }

    const good: StoredGood = {
        id: 'g_' + nextId(),
        name: body.name,
        description: body.description,
        price: body.price,
        stock: body.stock,
        createdAt: new Date().toISOString(),
    };
    const db = readDb();
    db.goods.push(good);
    writeDb(db);

    return NextResponse.json(good, { status: 201 });
}
