import { describe, expect, it, beforeEach } from 'vitest';
import { GET as listGoods, POST as createGood } from '@/app/api/goods/route';
import { GET as getGood, PUT as updateGood, DELETE as deleteGood } from '@/app/api/goods/[id]/route';
import { POST as loginPost } from '@/app/api/login/route';
import { useIsolatedDb, mockRequest } from './helpers';

describe('Goods CRUD', () => {
    useIsolatedDb();

    let token = '';
    beforeEach(async () => {
        const res = await loginPost(mockRequest({ body: { username: 'admin', password: 'admin123' } }));
        token = (await res.json()).token;
    });

    it('lists seeded goods', async () => {
        const res = await listGoods(mockRequest({ token }));
        expect(res.status).toBe(200);
        expect((await res.json()).length).toBeGreaterThanOrEqual(3);
    });

    it('creates, reads, updates and deletes a good', async () => {
        const created = await createGood(mockRequest({
            token,
            body: { name: 'Tea', description: 'Green tea 100g', price: 5, stock: 20 },
        }));
        expect(created.status).toBe(201);
        const g = await created.json();

        const got = await getGood(mockRequest({ token }), { params: Promise.resolve({ id: g.id }) });
        expect(got.status).toBe(200);

        const upd = await updateGood(
            mockRequest({ token, body: { price: 6 } }),
            { params: Promise.resolve({ id: g.id }) }
        );
        expect((await upd.json()).price).toBe(6);

        const del = await deleteGood(mockRequest({ token }), { params: Promise.resolve({ id: g.id }) });
        expect(del.status).toBe(204);
    });

    it('rejects unauthenticated writes', async () => {
        const res = await createGood(mockRequest({ body: { name: 'x', description: 'x', price: 1, stock: 1 } }));
        expect(res.status).toBe(401);
    });
});
