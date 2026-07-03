import { NextRequest, NextResponse } from 'next/server';
import { readDb, writeDb, nextId, StoredOrder, StoredOrderItem } from '@/lib/db';
import { requireAuth } from '@/lib/auth';

export async function GET(req: NextRequest): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    const db = readDb();
    return NextResponse.json(db.orders, { status: 200 });
}

export async function POST(req: NextRequest): Promise<NextResponse> {
    const auth = requireAuth(req);
    if (auth instanceof NextResponse) return auth;

    let body: { items?: { goodId: string; quantity: number }[] };
    try {
        body = await req.json();
    } catch {
        return NextResponse.json({ error: 'Invalid JSON body' }, { status: 400 });
    }

    if (!Array.isArray(body.items) || body.items.length === 0) {
        return NextResponse.json({ error: 'items must be a non-empty array' }, { status: 400 });
    }

    const db = readDb();

    const enriched: StoredOrderItem[] = [];
    let total = 0;
    for (const item of body.items) {
        const good = db.goods.find((g) => g.id === item.goodId);
        if (!good) {
            return NextResponse.json({ error: `Good not found: ${item.goodId}` }, { status: 400 });
        }
        if (item.quantity <= 0) {
            return NextResponse.json({ error: 'quantity must be > 0' }, { status: 400 });
        }
        enriched.push({ goodId: good.id, quantity: item.quantity, priceAtPurchase: good.price });
        total += good.price * item.quantity;
    }

    const order: StoredOrder = {
        id: 'o_' + nextId(),
        userId: auth.sub,
        items: enriched,
        total: Math.round(total * 100) / 100,
        status: 'pending',
        createdAt: new Date().toISOString(),
    };
    db.orders.push(order);
    writeDb(db);

    return NextResponse.json(order, { status: 201 });
}
