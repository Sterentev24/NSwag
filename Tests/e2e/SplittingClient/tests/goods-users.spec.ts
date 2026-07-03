import { test, expect } from '@playwright/test';

async function login(page: import('@playwright/test').Page) {
    await page.goto('/login');
    await page.getByLabel('Username').fill('admin');
    await page.getByLabel('Password').fill('admin123');
    await page.getByRole('button', { name: /sign in/i }).click();
    await expect(page).toHaveURL(/\/orders$/);
}

test.describe('Sidebar navigation', () => {
    test('navigates to Goods and Users pages via sidebar', async ({ page }) => {
        await login(page);

        await page.getByRole('link', { name: /goods/i }).click();
        await expect(page).toHaveURL(/\/goods$/);
        await expect(page.getByRole('heading', { name: 'Goods' })).toBeVisible();

        await page.getByRole('link', { name: /users/i }).click();
        await expect(page).toHaveURL(/\/users$/);
        await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible();
    });
});

test.describe('Goods CRUD via UI', () => {
    test('creates a new good and deletes it', async ({ page }) => {
        await login(page);
        await page.getByRole('link', { name: /goods/i }).click();

        const form = page.getByRole('form', { name: 'Create good' });
        await form.getByPlaceholder('name').fill('Milk');
        await form.getByPlaceholder('description').fill('Whole milk 1L');
        await form.getByPlaceholder('price').fill('1.5');
        await form.getByPlaceholder('stock').fill('30');
        await form.getByRole('button', { name: /create/i }).click();

        const table = page.getByRole('table', { name: 'Goods list' });
        const newRow = table.getByRole('row').filter({ hasText: 'Milk' });
        await expect(newRow).toBeVisible();

        await newRow.getByRole('button', { name: /delete/i }).click();
        await expect(newRow).not.toBeVisible();
    });
});

test.describe('Users CRUD via UI', () => {
    test('creates a new user', async ({ page }) => {
        await login(page);
        await page.getByRole('link', { name: /users/i }).click();

        const form = page.getByRole('form', { name: 'Create user' });
        await form.getByPlaceholder('username').fill('tempuser' + Date.now());
        await form.getByPlaceholder('email').fill('temp@example.com');
        await form.getByPlaceholder('password').fill('temp123');
        await form.getByRole('button', { name: /create/i }).click();

        const table = page.getByRole('table', { name: 'Users list' });
        await expect(table.getByText('temp@example.com')).toBeVisible();
    });
});
