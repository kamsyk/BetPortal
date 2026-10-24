import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
});

test('Microsoft login preserves the destination and opens sign-up for an unknown account', async ({ page }) => {
  let started = false;
  await page.route('http://localhost:5026/api/auth/microsoft?**', async route => {
    expect(new URL(route.request().url()).searchParams.get('returnUrl')).toBe('/advisor');
    started = true;
    await route.fulfill({
      status: 302,
      headers: {
        location: 'http://127.0.0.1:4300/login?microsoftError=account-required&returnUrl=%2Fadvisor',
      },
      body: '',
    });
  });
  await page.goto('/login?returnUrl=%2Fadvisor');
  await page.getByRole('button', { name: 'Sign in with Microsoft', exact: true }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Sign Up', exact: true })).toBeVisible();
  await expect(page.getByRole('alert')).toContainText('Microsoft');
  await expect(page.getByRole('button', { name: 'Sign up with Microsoft', exact: true })).toBeEnabled();
  expect(started).toBe(true);
  await page.getByRole('button', { name: 'Close', exact: true }).click();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page).not.toHaveURL(/microsoftError=/);
});

test('Microsoft sign-up reaches confirmation without issuing a portal session', async ({ page }) => {
  let started = false;
  await page.route('http://localhost:5026/api/auth/microsoft/register**', async route => {
    expect(new URL(route.request().url()).searchParams.get('returnUrl')).toBe('/advisor');
    started = true;
    await route.fulfill({
      status: 302,
      headers: { location: 'http://127.0.0.1:4300/sign-up-confirmation' },
      body: '',
    });
  });
  await page.goto('/sign-up?returnUrl=%2Fadvisor');
  await page.getByRole('button', { name: 'Sign up with Microsoft', exact: true }).click();
  await expect(page).toHaveURL(/\/sign-up-confirmation$/);
  await expect(page.getByRole('heading', { name: 'Registration received' })).toBeVisible();
  await expect(page.locator('.primary-nav')).toHaveCount(0);
  expect(started).toBe(true);
});

test('duplicate Microsoft registration keeps login available and does not open sign-up', async ({ page }) => {
  await page.goto('/login?microsoftError=account-exists&returnUrl=%2Fadvisor');
  await expect(page.getByRole('alert')).toContainText('already exists');
  await expect(page.getByRole('button', { name: 'Sign in with Microsoft', exact: true })).toBeEnabled();
  await expect(page.getByRole('button', { name: 'Sign up with Microsoft', exact: true })).toHaveCount(0);
});
