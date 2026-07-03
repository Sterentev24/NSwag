// Generates db.seed.json with real bcrypt hashes.
// Run:  pnpm run seed:generate    (produces db.seed.json in project root)
// Then commit the generated file — consumers do not need to run this again.

import bcrypt from 'bcryptjs';
import { writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, join } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const root = join(here, '..');

const usersPlain = [
    { id: 'u_admin', username: 'admin', email: 'admin@example.com', role: 'admin', password: 'admin123', createdAt: '2026-01-01T09:00:00.000Z' },
    { id: 'u_user1', username: 'user1', email: 'user1@example.com', role: 'user',  password: 'user123',  createdAt: '2026-01-02T10:00:00.000Z' },
];

const users = await Promise.all(usersPlain.map(async ({ password, ...rest }) => ({
    ...rest,
    passwordHash: await bcrypt.hash(password, 8),
})));

const seed = {
    users,
    goods: [
        { id: 'g_apple',  name: 'Apple',  description: 'Fresh red apple',    price: 0.5,  stock: 200, createdAt: '2026-01-01T09:00:00.000Z' },
        { id: 'g_bread',  name: 'Bread',  description: 'Whole grain loaf',   price: 2.0,  stock: 50,  createdAt: '2026-01-01T09:00:00.000Z' },
        { id: 'g_coffee', name: 'Coffee', description: 'Arabica beans 250g', price: 12.0, stock: 30,  createdAt: '2026-01-01T09:00:00.000Z' },
    ],
    orders: [
        {
            id: 'o_001',
            userId: 'u_user1',
            items: [
                { goodId: 'g_apple', quantity: 4, priceAtPurchase: 0.5 },
                { goodId: 'g_bread', quantity: 1, priceAtPurchase: 2.0 },
            ],
            total: 4.0,
            status: 'paid',
            createdAt: '2026-02-10T14:30:00.000Z',
        },
        {
            id: 'o_002',
            userId: 'u_user1',
            items: [
                { goodId: 'g_coffee', quantity: 2, priceAtPurchase: 12.0 },
            ],
            total: 24.0,
            status: 'pending',
            createdAt: '2026-02-15T11:15:00.000Z',
        },
    ],
};

writeFileSync(join(root, 'db.seed.json'), JSON.stringify(seed, null, 2) + '\n', 'utf8');
console.log('db.seed.json regenerated. admin=admin123, user1=user123');
