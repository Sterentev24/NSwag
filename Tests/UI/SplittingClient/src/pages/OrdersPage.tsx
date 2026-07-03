import { FormEvent, useEffect, useState } from 'react';
import { useApi } from '../api/clientFactory';
import { Order } from '../api/models/Order';
import { User } from '../api/models/User';
import { Good } from '../api/models/Good';
import { OrderStatus } from '../api/models/OrderStatus';

export function OrdersPage() {
    const { orders, users, goods } = useApi();
    const [orderList, setOrderList] = useState<Order[]>([]);
    const [userMap, setUserMap] = useState<Record<string, User>>({});
    const [goodMap, setGoodMap] = useState<Record<string, Good>>({});
    const [creating, setCreating] = useState(false);
    const [selectedGoodId, setSelectedGoodId] = useState('');
    const [quantity, setQuantity] = useState(1);
    const [error, setError] = useState<string | null>(null);

    const reload = async () => {
        const [os, us, gs] = await Promise.all([orders.list(), users.list(), goods.list()]);
        setOrderList(os);
        setUserMap(Object.fromEntries(us.map((u) => [u.id, u])));
        setGoodMap(Object.fromEntries(gs.map((g) => [g.id, g])));
        if (gs.length > 0 && !selectedGoodId) setSelectedGoodId(gs[0].id);
    };

    useEffect(() => {
        reload().catch((e) => setError(String(e)));
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    async function onCreate(e: FormEvent) {
        e.preventDefault();
        if (!selectedGoodId) return;
        setCreating(true);
        setError(null);
        try {
            await orders.create({ items: [{ goodId: selectedGoodId, quantity }] });
            await reload();
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        } finally {
            setCreating(false);
        }
    }

    async function onStatusChange(id: string, status: OrderStatus) {
        try {
            await orders.update(id, { status });
            await reload();
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        }
    }

    async function onDelete(id: string) {
        try {
            await orders.delete(id);
            await reload();
        } catch (err) {
            setError(err instanceof Error ? err.message : String(err));
        }
    }

    return (
        <section className="page">
            <h2>Orders</h2>

            <form onSubmit={onCreate} className="create-form" aria-label="Create order">
                <label>
                    Good:
                    <select value={selectedGoodId} onChange={(e) => setSelectedGoodId(e.target.value)}>
                        {Object.values(goodMap).map((g) => (
                            <option key={g.id} value={g.id}>{g.name} — ${g.price}</option>
                        ))}
                    </select>
                </label>
                <label>
                    Qty:
                    <input type="number" min={1} value={quantity} onChange={(e) => setQuantity(Number(e.target.value))} />
                </label>
                <button type="submit" className="btn btn--primary" disabled={creating || !selectedGoodId}>
                    {creating ? 'Creating…' : 'Create order'}
                </button>
            </form>

            {error && <p role="alert" className="error">{error}</p>}

            <table className="table" aria-label="Orders list">
                <thead>
                    <tr>
                        <th>ID</th><th>User</th><th>Items</th><th>Total</th><th>Status</th><th>Created</th><th></th>
                    </tr>
                </thead>
                <tbody>
                    {orderList.map((o) => (
                        <tr key={o.id}>
                            <td>{o.id}</td>
                            <td>{userMap[o.userId]?.username ?? o.userId}</td>
                            <td>
                                {o.items.map((it, i) => (
                                    <div key={i}>
                                        {goodMap[it.goodId]?.name ?? it.goodId} × {it.quantity} @ ${it.priceAtPurchase}
                                    </div>
                                ))}
                            </td>
                            <td>${o.total}</td>
                            <td>
                                <select
                                    value={o.status}
                                    onChange={(e) => onStatusChange(o.id, e.target.value as OrderStatus)}
                                    aria-label={`status of ${o.id}`}
                                >
                                    <option value="pending">pending</option>
                                    <option value="paid">paid</option>
                                    <option value="shipped">shipped</option>
                                    <option value="cancelled">cancelled</option>
                                </select>
                            </td>
                            <td>{new Date(o.createdAt).toLocaleString()}</td>
                            <td>
                                <button type="button" onClick={() => onDelete(o.id)} className="btn btn--danger">
                                    Delete
                                </button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </section>
    );
}
