import { expect, test } from '@playwright/test';

test('restores the menu after a full-page home redirect and on reload', async ({ page, context }, testInfo) => {
  await context.addCookies([{
    name: 'SessionId',
    value: 'synthetic-session',
    domain: 'localhost',
    path: '/',
    httpOnly: true,
    secure: true,
    sameSite: 'None',
  }]);
  let checks = 0;
  await page.route('http://localhost:5026/api/**', async (route) => {
    const { pathname } = new URL(route.request().url());
    const headers = await route.request().allHeaders();
    expect(headers['cookie']).toContain('SessionId=synthetic-session');
    if (pathname === '/api/auth/check') {
      checks++;
      await route.fulfill({
        json: { isAuthenticated: true, username: 'synthetic-user' },
      });
    } else if (pathname === '/api/auth/me') {
      await route.fulfill({
        json: { username: 'synthetic-user', fullName: 'Synthetic User', isAuthenticated: true },
      });
    } else {
      throw new Error(`Unexpected API request: ${pathname}`);
    }
  });

  for (const reload of [false, true]) {
    if (reload) {
      await page.reload();
    } else {
      await page.goto('/');
    }
    await expect(page).toHaveURL(/\/$/);
    if (testInfo.project.name === 'mobile-chromium') {
      await page.getByRole('button', { name: 'Open navigation menu' }).click();
      await expect(page.getByRole('menuitem', { name: /Advisor/ })).toBeVisible();
      await page.keyboard.press('Escape');
    } else {
      await expect(page.locator('.primary-nav').getByRole('link', { name: /Advisor/ })).toBeVisible();
    }
    await page.getByRole('button', { name: 'User profile', exact: true }).click();
    await expect(page.getByRole('menuitem', { name: 'Logout', exact: true })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Sign in', exact: true })).toHaveCount(0);
    await page.keyboard.press('Escape');
  }
  expect(checks).toBe(2);
});
