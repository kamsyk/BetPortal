import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
});

test('Facebook login preserves the destination and uses the login endpoint', async ({ page }) => {
  let loginStarted = false;
  await page.route('http://localhost:5026/api/auth/facebook?**', async route => {
    const url = new URL(route.request().url());
    expect(url.searchParams.get('returnUrl')).toBe('/advisor');
    loginStarted = true;
    await route.fulfill({
      status: 302,
      headers: { location: 'http://127.0.0.1:4300/login?facebookError=account-required&returnUrl=%2Fadvisor' },
      body: '',
    });
  });
  await page.goto('/login?returnUrl=%2Fadvisor');
  await page.getByRole('button', { name: 'Sign in with Facebook', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Sign up', exact: true })).toBeVisible();
  expect(loginStarted).toBe(true);
  await expect(page.getByRole('alert')).toContainText('Facebook');
  await page.keyboard.press('Escape');
  await expect(page.getByRole('dialog')).toHaveCount(0);
});

test('Facebook sign-up uses registration and reaches confirmation without signing in', async ({ page }) => {
  let registrationStarted = false;
  await page.route('http://localhost:5026/api/auth/facebook/register**', async route => {
    expect(new URL(route.request().url()).searchParams.get('returnUrl')).toBe('/advisor');
    registrationStarted = true;
    await route.fulfill({
      status: 302,
      headers: { location: 'http://127.0.0.1:4300/sign-up-confirmation' },
      body: '',
    });
  });
  await page.goto('/login?returnUrl=%2Fadvisor');
  await page.getByRole('button', { name: 'Sign up', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Sign up with Facebook', exact: true }).click();
  await expect(page).toHaveURL(/\/sign-up-confirmation$/);
  expect(registrationStarted).toBe(true);
  await expect(page.getByRole('heading', { name: 'Registration received' })).toBeVisible();
  await expect(page.locator('.primary-nav')).toHaveCount(0);
});
