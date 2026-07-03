import { Navigate, Route, Routes } from 'react-router-dom';
import { LoginPage } from './pages/LoginPage';
import { OrdersPage } from './pages/OrdersPage';
import { UsersPage } from './pages/UsersPage';
import { GoodsPage } from './pages/GoodsPage';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';

export function App() {
    return (
        <Routes>
            <Route path="/login" element={<LoginPage />} />
            <Route element={<ProtectedRoute />}>
                <Route element={<Layout />}>
                    <Route index element={<Navigate to="/orders" replace />} />
                    <Route path="/orders" element={<OrdersPage />} />
                    <Route path="/users" element={<UsersPage />} />
                    <Route path="/goods" element={<GoodsPage />} />
                </Route>
            </Route>
            <Route path="*" element={<Navigate to="/orders" replace />} />
        </Routes>
    );
}
