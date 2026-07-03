import { readFileSync, writeFileSync, existsSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';

export type StoredUser = {
    id: string;
    username: string;
    email: string;
    role: 'admin' | 'user';
    passwordHash: string;
    createdAt: string;
};

export type StoredGood = {
    id: string;
    name: string;
    description: string;
    price: number;
    stock: number;
    createdAt: string;
};

export type StoredOrderItem = {
    goodId: string;
    quantity: number;
    priceAtPurchase: number;
};

export type StoredOrder = {
    id: string;
    userId: string;
    items: StoredOrderItem[];
    total: number;
    status: 'pending' | 'paid' | 'shipped' | 'cancelled';
    createdAt: string;
};

export type DbShape = {
    users: StoredUser[];
    goods: StoredGood[];
    orders: StoredOrder[];
};

const DEFAULT_DB_PATH = join(process.cwd(), 'db.json');
const SEED_PATH = join(process.cwd(), 'db.seed.json');

function dbPath(): string {
    return process.env.DB_PATH || DEFAULT_DB_PATH;
}

function ensureExists(): void {
    const path = dbPath();
    if (existsSync(path)) return;

    mkdirSync(dirname(path), { recursive: true });
    if (existsSync(SEED_PATH)) {
        writeFileSync(path, readFileSync(SEED_PATH, 'utf8'), 'utf8');
    } else {
        writeFileSync(path, JSON.stringify({ users: [], goods: [], orders: [] }, null, 2), 'utf8');
    }
}

export function readDb(): DbShape {
    ensureExists();
    const raw = readFileSync(dbPath(), 'utf8');
    return JSON.parse(raw) as DbShape;
}

export function writeDb(db: DbShape): void {
    ensureExists();
    writeFileSync(dbPath(), JSON.stringify(db, null, 2), 'utf8');
}

export function nextId(): string {
    return Math.random().toString(36).slice(2, 10) + Date.now().toString(36);
}

export function stripPasswordHash(user: StoredUser) {
    const { passwordHash: _hash, ...safe } = user;
    return safe;
}
