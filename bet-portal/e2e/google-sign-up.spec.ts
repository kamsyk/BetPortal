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
