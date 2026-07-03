import { test, expect } from '@playwright/test';

async function login(page: import('@playwright/test').Page) {
    await page.goto('/login');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('admin123');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/orders$/);
}

test.describe('Orders CRUD via UI', () => {
    test('renders seeded orders and creates a new one', async ({ page }) => {
        await login(page);

        const table = page.getByRole('table', { name: 'Orders list' });
        await expect(table).toBeVisible();
        await expect(table.getByText('o_001')).toBeVisible();

        // Create a new order with the first good in the select and qty=2.
        await page.getByRole('form', { name: 'Create order' }).getByRole('button', { name: /create order/i }).click();

        // A new row appears (at least one new order id starting with o_ but not o_001/o_002).
        await expect.poll(async () => (await table.getByText(/^o_[A-Za-z0-9]+/).count())).toBeGreaterThan(2);
    });

    test('changes order status', async ({ page }) => {
        await login(page);
        const statusSelect = page.getByLabel('status of o_002');
        await statusSelect.selectOption('shipped');
        await expect(statusSelect).toHaveValue('shipped');
    });
});
