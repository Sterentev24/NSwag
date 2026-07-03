import { describe, expect, it } from 'vitest';
import { POST as loginPost } from '@/app/api/login/route';
import { useIsolatedDb, mockRequest } from './helpers';

describe('POST /api/login', () => {
    useIsolatedDb();

    it('returns a JWT for valid credentials', async () => {
        const res = await loginPost(mockRequest({ body: { username: 'admin', password: 'admin123' } }));
        expect(res.status).toBe(200);
        const json = await res.json();
        expect(typeof json.token).toBe('string');
        expect(json.user.username).toBe('admin');
        expect(json.user).not.toHaveProperty('passwordHash');
    });

    it('rejects wrong password', async () => {
        const res = await loginPost(mockRequest({ body: { username: 'admin', password: 'wrong' } }));
        expect(res.status).toBe(401);
    });

    it('rejects unknown user', async () => {
        const res = await loginPost(mockRequest({ body: { username: 'ghost', password: 'x' } }));
        expect(res.status).toBe(401);
    });

    it('rejects missing fields', async () => {
        const res = await loginPost(mockRequest({ body: { username: 'admin' } }));
        expect(res.status).toBe(400);
    });
});
