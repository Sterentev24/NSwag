import { FormEvent, useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../auth/AuthContext';
import { useApi } from '../api/clientFactory';

type LocationState = { from?: { pathname?: string } };

export function LoginPage() {
    const { login } = useAuth();
    const { auth } = useApi();
    const navigate = useNavigate();
    const location = useLocation();
    const from = ((location.state as LocationState | null)?.from?.pathname) ?? '/orders';

    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [submitting, setSubmitting] = useState(false);

    async function onSubmit(e: FormEvent) {
        e.preventDefault();
        setError(null);
        setSubmitting(true);
        try {
            const res = await auth.login({ username, password });
            login(res.token, res.user);
            navigate(from, { replace: true });
        } catch (err) {
            const message = err instanceof Error ? err.message : 'Login failed';
            setError(message);
        } finally {
            setSubmitting(false);
        }
    }

    return (
        <div className="login">
            <form onSubmit={onSubmit} className="login__form" aria-label="Login form">
                <h2>Sign in</h2>
                <label>
                    <span>Username</span>
                    <input
                        type="text"
                        value={username}
                        onChange={(e) => setUsername(e.target.value)}
                        autoFocus
                        required
                    />
                </label>
                <label>
                    <span>Password</span>
                    <input
                        type="password"
                        value={password}
                        onChange={(e) => setPassword(e.target.value)}
                        required
                    />
                </label>
                {error && <p role="alert" className="login__error">{error}</p>}
                <button type="submit" disabled={submitting} className="btn btn--primary">
                    {submitting ? 'Signing in…' : 'Sign in'}
                </button>
                <p className="login__hint">Try <code>admin / admin123</code> or <code>user1 / user123</code>.</p>
            </form>
        </div>
    );
}
