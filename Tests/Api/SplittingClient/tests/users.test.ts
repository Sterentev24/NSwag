import { describe, expect, it, beforeEach } from 'vitest';
import { GET as listUsers, POST as createUser } from '@/app/api/users/route';
import { GET as getUser, PUT as updateUser, DELETE as deleteUser } from '@/app/api/users/[id]/route';
import { POST as loginPost } from '@/app/api/login/route';
import { useIsolatedDb, mockRequest } from './helpers';

describe('Users CRUD', () => {
    useIsolatedDb();

    let token = '';
    beforeEach(async () => {
        const res = await loginPost(mockRequest({ body: { username: 'admin', password: 'admin123' } }));
        token = (await res.json()).token;
    });

    it('lists seeded users', async () => {
        const res = await listUsers(mockRequest({ token }));
        expect(res.status).toBe(200);
        const list = await res.json();
        expect(list.length).toBeGreaterThanOrEqual(2);
        expect(list.every((u: { passwordHash?: string }) => u.passwordHash === undefined)).toBe(true);
    });

    it('requires auth', async () => {
        const res = await listUsers(mockRequest({}));
        expect(res.status).toBe(401);
    });

    it('creates, reads, updates and deletes a user', async () => {
        const created = await createUser(mockRequest({
            token,
            body: { username: 'newbie', password: 'pw', email: 'n@e.com', role: 'user' },
        }));
        expect(created.status).toBe(201);
        const cJson = await created.json();
        expect(cJson.username).toBe('newbie');

        const got = await getUser(mockRequest({ token }), { params: Promise.resolve({ id: cJson.id }) });
        expect(got.status).toBe(200);

        const updated = await updateUser(
            mockRequest({ token, body: { email: 'n2@e.com' } }),
            { params: Promise.resolve({ id: cJson.id }) }
        );
        expect(updated.status).toBe(200);
        expect((await updated.json()).email).toBe('n2@e.com');

        const deleted = await deleteUser(mockRequest({ token }), { params: Promise.resolve({ id: cJson.id }) });
        expect(deleted.status).toBe(204);

        const notFound = await getUser(mockRequest({ token }), { params: Promise.resolve({ id: cJson.id }) });
        expect(notFound.status).toBe(404);
    });

    it('rejects duplicate username', async () => {
        const res = await createUser(mockRequest({
            token,
            body: { username: 'admin', password: 'x', email: 'a@a.com', role: 'user' },
        }));
        expect(res.status).toBe(409);
    });
});
