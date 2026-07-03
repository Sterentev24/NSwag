import jwt from 'jsonwebtoken';
import { NextRequest, NextResponse } from 'next/server';

const SECRET = process.env.JWT_SECRET || 'demo-secret-do-not-use-in-production';
const EXPIRES_IN = '2h';

export type TokenPayload = {
    sub: string;
    username: string;
    role: 'admin' | 'user';
};

export function signToken(payload: TokenPayload): string {
    return jwt.sign(payload, SECRET, { expiresIn: EXPIRES_IN });
}

export function verifyToken(token: string): TokenPayload | null {
    try {
        return jwt.verify(token, SECRET) as TokenPayload;
    } catch {
        return null;
    }
}

export function requireAuth(req: NextRequest): TokenPayload | NextResponse {
    const header = req.headers.get('authorization') || '';
    const [scheme, token] = header.split(' ');
    if (scheme !== 'Bearer' || !token) {
        return NextResponse.json({ error: 'Unauthorized' }, { status: 401 });
    }
    const payload = verifyToken(token);
    if (!payload) {
        return NextResponse.json({ error: 'Invalid or expired token' }, { status: 401 });
    }
    return payload;
}
