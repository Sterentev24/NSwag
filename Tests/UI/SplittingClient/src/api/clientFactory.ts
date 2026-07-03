// Constructs the NSwag-generated split clients wired up with the current AuthContext token.

import { useMemo } from 'react';
import { AuthClient } from './clients/AuthClient';
import { UsersClient } from './clients/UsersClient';
import { GoodsClient } from './clients/GoodsClient';
import { OrdersClient } from './clients/OrdersClient';
import { useAuth } from '../auth/AuthContext';
import { createHttp } from './httpFactory';

// Matches the `servers[0].url` in openapi.json. Requests go through Vite proxy → localhost:3001.
const BASE_URL = '/api';

export function useApi() {
    const { token } = useAuth();
    return useMemo(() => {
        const http = createHttp(() => token);
        return {
            auth: new AuthClient(BASE_URL, http),
            users: new UsersClient(BASE_URL, http),
            goods: new GoodsClient(BASE_URL, http),
            orders: new OrdersClient(BASE_URL, http),
        };
    }, [token]);
}
