import { describe, expect, it, beforeEach } from 'vitest';
import { GET as listOrders, POST as createOrder } from '@/app/api/orders/route';
import { PUT as updateOrder, DELETE as deleteOrder } from '@/app/api/orders/[id]/route';
import { POST as loginPost } from '@/app/api/login/route';
import { useIsolatedDb, mockRequest } from './helpers';

describe('Orders CRUD', () => {
    useIsolatedDb();

    let token = '';
    beforeEach(async () => {
        const res = await loginPost(mockRequest({ body: { username: 'user1', password: 'user123' } }));
        token = (await res.json()).token;
    });

    it('computes total from good prices', async () => {
        // Apple $0.5 × 4 + Coffee $12 × 1 = 14
        const res = await createOrder(mockRequest({
            token,
            body: { items: [
                { goodId: 'g_apple',  quantity: 4 },
                { goodId: 'g_coffee', quantity: 1 },
            ] },
        }));
        expect(res.status).toBe(201);
        const order = await res.json();
        expect(order.total).toBe(14);
        expect(order.status).toBe('pending');
        expect(order.items).toHaveLength(2);
        expect(order.items[0].priceAtPurchase).toBe(0.5);
    });

    it('rejects unknown good', async () => {
        const res = await createOrder(mockRequest({
            token,
            body: { items: [{ goodId: 'g_nope', quantity: 1 }] },
        }));
        expect(res.status).toBe(400);
    });

    it('rejects non-positive quantity', async () => {
        const res = await createOrder(mockRequest({
            token,
            body: { items: [{ goodId: 'g_apple', quantity: 0 }] },
        }));
        expect(res.status).toBe(400);
    });

    it('updates status', async () => {
        const list = await listOrders(mockRequest({ token }));
        const order = (await list.json())[0];

        const upd = await updateOrder(
            mockRequest({ token, body: { status: 'shipped' } }),
            { params: Promise.resolve({ id: order.id }) }
        );
        expect((await upd.json()).status).toBe('shipped');
    });

    it('deletes an order', async () => {
        const created = await createOrder(mockRequest({
            token,
            body: { items: [{ goodId: 'g_bread', quantity: 1 }] },
        }));
        const order = await created.json();

        const del = await deleteOrder(mockRequest({ token }), { params: Promise.resolve({ id: order.id }) });
        expect(del.status).toBe(204);
    });
});
