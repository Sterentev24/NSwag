import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../src/auth/AuthContext';
import { LoginPage } from '../src/pages/LoginPage';

// The page uses AuthClient via useApi() — we stub window.fetch so the request never leaves the harness.
function stubFetch(response: Response) {
    global.fetch = vi.fn(async () => response) as unknown as typeof fetch;
}

function jsonResponse(status: number, body: unknown): Response {
    return new Response(JSON.stringify(body), {
        status,
        headers: { 'Content-Type': 'application/json' },
    });
}

describe('LoginPage', () => {
    beforeEach(() => localStorage.clear());

    it('submits credentials and redirects on success', async () => {
        stubFetch(jsonResponse(200, {
            token: 'tk-abc',
            user: { id: 'u_1', username: 'admin', email: 'a@e.com', role: 'admin', createdAt: '2026-01-01T00:00:00Z' },
        }));

        render(
            <MemoryRouter initialEntries={['/login']}>
                <AuthProvider>
                    <Routes>
                        <Route path="/login" element={<LoginPage />} />
                        <Route path="/orders" element={<div>orders page</div>} />
                    </Routes>
                </AuthProvider>
            </MemoryRouter>
        );

        const user = userEvent.setup();
        await user.type(screen.getByLabelText('Username'), 'admin');
        await user.type(screen.getByLabelText('Password'), 'admin123');
        await user.click(screen.getByRole('button', { name: /sign in/i }));

        await waitFor(() => expect(screen.getByText('orders page')).toBeInTheDocument());
        expect(localStorage.getItem('splitting-client-session')).toContain('tk-abc');
    });

    it('shows error text on 401', async () => {
        stubFetch(jsonResponse(401, { error: 'Invalid credentials' }));

        render(
            <MemoryRouter>
                <AuthProvider>
                    <LoginPage />
                </AuthProvider>
            </MemoryRouter>
        );

        const user = userEvent.setup();
        await user.type(screen.getByLabelText('Username'), 'admin');
        await user.type(screen.getByLabelText('Password'), 'wrong');
        await user.click(screen.getByRole('button', { name: /sign in/i }));

        await waitFor(() => expect(screen.getByRole('alert')).toBeInTheDocument());
    });
});
