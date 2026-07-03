import { FormEvent, useEffect, useState } from 'react';
import { useApi } from '../api/clientFactory';
import { User } from '../api/models/User';
import { CreateUserRequestRole } from '../api/models/CreateUserRequestRole';

export function UsersPage() {
    const { users } = useApi();
    const [list, setList] = useState<User[]>([]);
    const [username, setUsername] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [role, setRole] = useState<CreateUserRequestRole>(CreateUserRequestRole.User);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const reload = () => users.list().then(setList).catch((e) => setError(String(e)));
    useEffect(() => { reload(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

    async function onCreate(e: FormEvent) {
        e.preventDefault();
        setBusy(true); setError(null);
        try {
            await users.create({ username, email, password, role });
            setUsername(''); setEmail(''); setPassword('');
            await reload();
        } catch (err) { setError(err instanceof Error ? err.message : String(err)); }
        finally { setBusy(false); }
    }

    async function onDelete(id: string) {
        try { await users.delete(id); await reload(); }
        catch (err) { setError(err instanceof Error ? err.message : String(err)); }
    }

    return (
        <section className="page">
            <h2>Users</h2>

            <form onSubmit={onCreate} className="create-form" aria-label="Create user">
                <input placeholder="username" value={username} onChange={(e) => setUsername(e.target.value)} required />
                <input placeholder="email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
                <input placeholder="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
                <select value={role} onChange={(e) => setRole(e.target.value as CreateUserRequestRole)}>
                    <option value={CreateUserRequestRole.User}>user</option>
                    <option value={CreateUserRequestRole.Admin}>admin</option>
                </select>
                <button type="submit" className="btn btn--primary" disabled={busy}>Create</button>
            </form>

            {error && <p role="alert" className="error">{error}</p>}

            <table className="table" aria-label="Users list">
                <thead><tr><th>ID</th><th>Username</th><th>Email</th><th>Role</th><th>Created</th><th></th></tr></thead>
                <tbody>
                    {list.map((u) => (
                        <tr key={u.id}>
                            <td>{u.id}</td>
                            <td>{u.username}</td>
                            <td>{u.email}</td>
                            <td>{u.role}</td>
                            <td>{new Date(u.createdAt).toLocaleString()}</td>
                            <td><button type="button" onClick={() => onDelete(u.id)} className="btn btn--danger">Delete</button></td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </section>
    );
}
