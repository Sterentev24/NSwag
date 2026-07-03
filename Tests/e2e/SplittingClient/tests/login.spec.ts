import { test, expect } from '@playwright/test';

test.describe('Login flow', () => {
    test('successful login redirects to /orders', async ({ page }) => {
        await page.goto('/login');
        await page.getByLabel('Username').fill('admin');
        await page.getByLabel('Password').fill('admin123');
        await page.getByRole('button', { name: /sign in/i }).click();

        await expect(page).toHaveURL(/\/orders$/);
        await expect(page.getByRole('heading', { name: 'Orders' })).toBeVisible();
        await expect(page.getByTestId('username')).toHaveText('admin');
    });

    test('wrong password stays on login with error', async ({ page }) => {
        await page.goto('/login');
        await page.getByLabel('Username').fill('admin');
        await page.getByLabel('Password').fill('wrong');
        await page.getByRole('button', { name: /sign in/i }).click();

        await expect(page.getByRole('alert')).toBeVisible();
        await expect(page).toHaveURL(/\/login$/);
    });

    test('accessing a protected route without token redirects to /login', async ({ page }) => {
        await page.goto('/orders');
        await expect(page).toHaveURL(/\/login$/);
    });
});
