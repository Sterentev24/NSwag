// Thin wrapper that adds the Bearer token to every request.
// Wraps window.fetch so the NSwag-generated Fetch clients pick it up via their `http` constructor arg.

export type ApiHttp = { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };

export function createHttp(getToken: () => string | null): ApiHttp {
    return {
        fetch(url, init) {
            const headers = new Headers(init?.headers);
            const token = getToken();
            if (token && !headers.has('Authorization')) {
                headers.set('Authorization', `Bearer ${token}`);
            }
            return window.fetch(url, { ...init, headers });
        },
    };
}
