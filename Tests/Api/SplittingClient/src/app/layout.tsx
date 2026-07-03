export const metadata = {
    title: 'Splitting Client Demo API',
    description: 'Demo API used to validate NSwag SplitByDto TypeScript client',
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
    return (
        <html lang="en">
            <body>{children}</body>
        </html>
    );
}
