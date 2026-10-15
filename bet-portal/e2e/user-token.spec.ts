import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
});

test('activates using POST and replaces the activation URL with Welcome', async ({ page }) => {
  let activations = 0;
  await page.route('http://localhost:5026/api/user-token', async route => {
    expect(route.request().method()).toBe('POST');
    expect(route.request().postDataJSON()).toEqual({ token: 'synthetic-token' });
    activations++;
    await route.fulfill({ status: 204 });
  });
  await page.goto('/user-token?token=synthetic-token');
  await expect(page).toHaveURL(/\/welcome$/);
  await expect(page.getByRole('heading', { name: 'Welcome to Bet Portal' })).toBeVisible();
  await expect(page.getByText('Your account is active. You can now sign in.')).toBeVisible();
  expect(activations).toBe(1);
  await page.getByRole('main').getByRole('link', { name: 'Sign in' }).click();
  await expect(page).toHaveURL(/\/login$/);
});

test('shows invalid-token error instead of Welcome', async ({ page }) => {
  await page.route('http://localhost:5026/api/user-token', route =>
    route.fulfill({ status: 404, json: { title: 'Activation token is invalid or already used' } }));
  await page.goto('/user-token?token=used-token');
  await expect(page.getByRole('alert')).toHaveText('This activation link is invalid or has already been used.');
  await expect(page).toHaveURL(/\/user-token\?token=used-token$/);
});
