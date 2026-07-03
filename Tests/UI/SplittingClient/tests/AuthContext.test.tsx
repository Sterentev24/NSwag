import { describe, it, expect, beforeEach } from 'vitest';
import { act, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AuthProvider, useAuth } from '../src/auth/AuthContext';
import { User } from '../src/api/models/User';

function Probe() {
    const { token, user, login, logout } = useAuth();
    const testUser: User = {
        id: 'u_1', username: 'tester', email: 't@e.com', role: 'admin' as User['role'], createdAt: '2026-01-01T00:00:00Z',
    };
    return (
        <div>
            <span data-testid="token">{token ?? '(none)'}</span>
            <span data-testid="user">{user?.username ?? '(none)'}</span>
            <button onClick={() => login('tk-123', testUser)}>Login</button>
            <button onClick={logout}>Logout</button>
        </div>
    );
}

describe('AuthContext', () => {
    beforeEach(() => localStorage.clear());

    it('starts empty when no session in storage', () => {
        render(<AuthProvider><Probe /></AuthProvider>);
        expect(screen.getByTestId('token').textContent).toBe('(none)');
        expect(screen.getByTestId('user').textContent).toBe('(none)');
    });

    it('login stores session and updates state', async () => {
        const user = userEvent.setup();
        render(<AuthProvider><Probe /></AuthProvider>);
        await user.click(screen.getByRole('button', { name: 'Login' }));
        expect(screen.getByTestId('token').textContent).toBe('tk-123');
        expect(screen.getByTestId('user').textContent).toBe('tester');
        expect(localStorage.getItem('splitting-client-session')).toContain('tk-123');
    });

    it('logout clears storage', async () => {
        localStorage.setItem('splitting-client-session', JSON.stringify({ token: 'x', user: { username: 'y' } }));
        const user = userEvent.setup();
        render(<AuthProvider><Probe /></AuthProvider>);
        expect(screen.getByTestId('token').textContent).toBe('x');
        await act(async () => { await user.click(screen.getByRole('button', { name: 'Logout' })); });
        expect(screen.getByTestId('token').textContent).toBe('(none)');
        expect(localStorage.getItem('splitting-client-session')).toBeNull();
    });
});
