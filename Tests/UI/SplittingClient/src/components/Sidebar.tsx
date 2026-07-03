import { NavLink } from 'react-router-dom';
import { Users, Package, ShoppingCart } from 'lucide-react';

type Props = { open: boolean };

const links = [
    { to: '/orders', icon: ShoppingCart, label: 'Orders' },
    { to: '/goods', icon: Package, label: 'Goods' },
    { to: '/users', icon: Users, label: 'Users' },
];

export function Sidebar({ open }: Props) {
    return (
        <aside className={`sidebar ${open ? 'sidebar--open' : ''}`} data-testid="sidebar">
            <nav>
                <ul>
                    {links.map(({ to, icon: Icon, label }) => (
                        <li key={to}>
                            <NavLink
                                to={to}
                                className={({ isActive }) => (isActive ? 'sidebar__link sidebar__link--active' : 'sidebar__link')}
                            >
                                <Icon size={20} aria-hidden />
                                <span className="sidebar__label">{label}</span>
                            </NavLink>
                        </li>
                    ))}
                </ul>
            </nav>
        </aside>
    );
}
