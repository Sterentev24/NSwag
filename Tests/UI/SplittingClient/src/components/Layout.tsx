import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Menu } from 'lucide-react';
import { Sidebar } from './Sidebar';
import { useAuth } from '../auth/AuthContext';

export function Layout() {
    const [sidebarOpen, setSidebarOpen] = useState(true);
    const { user, logout } = useAuth();

    return (
        <div className="app">
            <header className="app__header">
                <button
                    type="button"
                    aria-label="Toggle menu"
                    className="hamburger"
                    onClick={() => setSidebarOpen((v) => !v)}
                >
                    <Menu size={22} />
                </button>
                <h1 className="app__title">Splitting Client Demo</h1>
                <div className="app__user">
                    {user && <span data-testid="username">{user.username}</span>}
                    <button type="button" onClick={logout} className="btn btn--ghost">Logout</button>
                </div>
            </header>
            <div className="app__body">
                <Sidebar open={sidebarOpen} />
                <main className="app__content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}
