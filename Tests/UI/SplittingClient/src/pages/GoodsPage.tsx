import { FormEvent, useEffect, useState } from 'react';
import { useApi } from '../api/clientFactory';
import { Good } from '../api/models/Good';

export function GoodsPage() {
    const { goods } = useApi();
    const [list, setList] = useState<Good[]>([]);
    const [name, setName] = useState('');
    const [description, setDescription] = useState('');
    const [price, setPrice] = useState(0);
    const [stock, setStock] = useState(0);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const reload = () => goods.list().then(setList).catch((e) => setError(String(e)));
    useEffect(() => { reload(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

    async function onCreate(e: FormEvent) {
        e.preventDefault();
        setBusy(true); setError(null);
        try {
            await goods.create({ name, description, price, stock });
            setName(''); setDescription(''); setPrice(0); setStock(0);
            await reload();
        } catch (err) { setError(err instanceof Error ? err.message : String(err)); }
        finally { setBusy(false); }
    }

    async function onDelete(id: string) {
        try { await goods.delete(id); await reload(); }
        catch (err) { setError(err instanceof Error ? err.message : String(err)); }
    }

    return (
        <section className="page">
            <h2>Goods</h2>

            <form onSubmit={onCreate} className="create-form" aria-label="Create good">
                <input placeholder="name" value={name} onChange={(e) => setName(e.target.value)} required />
                <input placeholder="description" value={description} onChange={(e) => setDescription(e.target.value)} required />
                <input type="number" step="0.01" placeholder="price" value={price} onChange={(e) => setPrice(Number(e.target.value))} required />
                <input type="number" placeholder="stock" value={stock} onChange={(e) => setStock(Number(e.target.value))} required />
                <button type="submit" className="btn btn--primary" disabled={busy}>Create</button>
            </form>

            {error && <p role="alert" className="error">{error}</p>}

            <table className="table" aria-label="Goods list">
                <thead><tr><th>ID</th><th>Name</th><th>Description</th><th>Price</th><th>Stock</th><th>Created</th><th></th></tr></thead>
                <tbody>
                    {list.map((g) => (
                        <tr key={g.id}>
                            <td>{g.id}</td>
                            <td>{g.name}</td>
                            <td>{g.description}</td>
                            <td>${g.price}</td>
                            <td>{g.stock}</td>
                            <td>{new Date(g.createdAt).toLocaleString()}</td>
                            <td><button type="button" onClick={() => onDelete(g.id)} className="btn btn--danger">Delete</button></td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </section>
    );
}
