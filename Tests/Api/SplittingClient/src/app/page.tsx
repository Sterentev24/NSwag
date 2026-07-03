export default function Home() {
    return (
        <main style={{ fontFamily: 'sans-serif', padding: 24 }}>
            <h1>Splitting Client Demo API</h1>
            <p>OpenAPI spec: <a href="/api/openapi">/api/openapi</a></p>
            <p>Login (POST): <code>/api/login</code></p>
            <p>Resources: <code>/api/users</code>, <code>/api/goods</code>, <code>/api/orders</code></p>
        </main>
    );
}
