import { createContext, useCallback, useContext, useMemo, useState, ReactNode } from 'react';
import { User } from '../api/models/User';

type StoredSession = { token: string; user: User };

type AuthContextValue = {
    token: string | null;
    user: User | null;
    login: (token: string, user: User) => void;
    logout: () => void;
};

const AuthContext = createContext<AuthContextValue | null>(null);

const STORAGE_KEY = 'splitting-client-session';

function loadSession(): StoredSession | null {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);
        return raw ? (JSON.parse(raw) as StoredSession) : null;
    } catch {
        return null;
    }
}

export function AuthProvider({ children }: { children: ReactNode }) {
    const [session, setSession] = useState<StoredSession | null>(() => loadSession());

    const login = useCallback((token: string, user: User) => {
        const next = { token, user };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
        setSession(next);
    }, []);

    const logout = useCallback(() => {
        localStorage.removeItem(STORAGE_KEY);
        setSession(null);
    }, []);

    const value = useMemo<AuthContextValue>(
        () => ({ token: session?.token ?? null, user: session?.user ?? null, login, logout }),
        [session, login, logout]
    );

    return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
    return ctx;
}
