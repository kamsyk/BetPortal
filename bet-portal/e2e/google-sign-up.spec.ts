import { expect, test } from '@playwright/test';

test('opens Sign up automatically for an unknown Google account and allows dismissal', async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
  await page.goto('/login?googleError=account-required&returnUrl=%2Fadvisor');
  const dialog = page.getByRole('dialog', { name: 'Sign up', exact: true });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Email', { exact: true })).toBeVisible();
  await expect(page).toHaveURL(/returnUrl=%2Fadvisor/);
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await page.getByRole('button', { name: 'Sign up', exact: true }).click();
  await expect(dialog).toBeVisible();
});

test('keeps Sign up closed on a normal login visit', async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
  await page.goto('/login');
  await expect(page.getByRole('button', { name: 'Sign up', exact: true })).toBeVisible();
  await expect(page.getByRole('dialog')).toHaveCount(0);
});

test('Sign up with Google uses registration and reaches confirmation without a signed-in menu', async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
  let registrationStarted = false;
  await page.route('http://localhost:5026/api/auth/google/register**', async route => {
    registrationStarted = true;
    await route.fulfill({
      status: 302,
      headers: { location: 'http://127.0.0.1:4300/sign-up-confirmation' },
      body: '',
    });
  });
  await page.goto('/login');
  await page.getByRole('button', { name: 'Sign up', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Sign up with Google', exact: true }).click();
  await expect(page).toHaveURL(/\/sign-up-confirmation$/);
  expect(registrationStarted).toBe(true);
  await expect(page.getByRole('heading', { name: 'Registration received' })).toBeVisible();
  await expect(page.getByText('Your account has been created and is awaiting activation. You are not signed in yet.')).toBeVisible();
  await expect(page.locator('.primary-nav')).toHaveCount(0);
});
