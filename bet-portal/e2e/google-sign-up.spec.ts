import { expect, test } from '@playwright/test';

test('opens Sign up page automatically for an unknown Google account', async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
  await page.goto('/login?googleError=account-required&returnUrl=%2Fadvisor');
  await expect(page).toHaveURL(/\/sign-up\?.*googleError=account-required/);
  await expect(page.getByRole('heading', { name: 'Sign Up', exact: true })).toBeVisible();
  await expect(page.getByLabel('Email', { exact: true })).toBeVisible();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('alert')).toContainText('Google');
});

test('keeps Sign up closed on a normal login visit', async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
  await page.goto('/login');
  await expect(page.getByRole('button', { name: 'Sign up', exact: true })).toBeVisible();
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page).toHaveURL(/\/login$/);
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
  await page.goto('/sign-up');
  await page.getByRole('button', { name: 'Sign up with Google', exact: true }).click();
  await expect(page).toHaveURL(/\/sign-up-confirmation$/);
  expect(registrationStarted).toBe(true);
  await expect(page.getByRole('heading', { name: 'Registration received' })).toBeVisible();
  await expect(page.getByText('Your account has been created. Check your email for the welcome message and use its link to activate your account. You are not signed in yet.')).toBeVisible();
  await expect(page.locator('.primary-nav')).toHaveCount(0);
});

test('login Sign up button navigates to the sign-up page instead of a dialog', async ({ page }) => {
  await page.route('http://localhost:5026/api/auth/check', route =>
    route.fulfill({ json: { isAuthenticated: false } }));
  await page.goto('/login');
  await page.getByRole('button', { name: 'Sign up', exact: true }).click();
  await expect(page).toHaveURL(/\/sign-up$/);
  await expect(page.getByRole('dialog')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Sign Up', exact: true })).toBeVisible();
});
