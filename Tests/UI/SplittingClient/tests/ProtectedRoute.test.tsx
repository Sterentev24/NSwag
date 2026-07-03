import { describe, it, expect, beforeEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AuthProvider } from '../src/auth/AuthContext';
import { ProtectedRoute } from '../src/components/ProtectedRoute';

function render_(initial: string) {
    return render(
        <MemoryRouter initialEntries={[initial]}>
            <AuthProvider>
                <Routes>
                    <Route path="/login" element={<div>login page</div>} />
                    <Route element={<ProtectedRoute />}>
                        <Route path="/secret" element={<div>secret content</div>} />
                    </Route>
                </Routes>
            </AuthProvider>
        </MemoryRouter>
    );
}

describe('ProtectedRoute', () => {
    beforeEach(() => localStorage.clear());

    it('redirects to /login when there is no token', () => {
        render_('/secret');
        expect(screen.getByText('login page')).toBeInTheDocument();
        expect(screen.queryByText('secret content')).not.toBeInTheDocument();
    });

    it('renders child route when token is present', () => {
        localStorage.setItem('splitting-client-session', JSON.stringify({
            token: 'tk',
            user: { id: 'u', username: 'x', email: 'x@x.com', role: 'user', createdAt: '2026-01-01T00:00:00Z' },
        }));
        render_('/secret');
        expect(screen.getByText('secret content')).toBeInTheDocument();
    });
});
