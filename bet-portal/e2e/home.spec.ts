import { expect, test } from '@playwright/test';

test.beforeEach(async ({ page }) => {
  await page.route('http://localhost:5026/api/**', async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ isAuthenticated: false }),
    });
  });
  await page.goto('/');
});

test('welcomes anonymous visitors and keeps all sections reachable without horizontal overflow', async ({ page }) => {
  await expect(page).toHaveURL(/\/$/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Your portal data. A clearer perspective.');
  await expect(page.getByRole('article')).toHaveCount(3);
  await page.getByRole('link', { name: 'Explore the portal' }).click();
  await expect(page.getByRole('heading', { name: 'Meet your portal' })).toBeInViewport();
  await page.getByRole('link', { name: 'Sign in to get started' }).scrollIntoViewIfNeeded();
  await expect(page.getByRole('link', { name: 'Sign in to get started' })).toBeInViewport();
  expect(await page.locator('main').evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  await page.getByText('AI responses can contain mistakes.', { exact: false }).scrollIntoViewIfNeeded();
  await expect(page.getByText('AI responses can contain mistakes.', { exact: false })).toBeInViewport();
});

test('takes anonymous advisor visitors to sign-in with their destination preserved', async ({ page }) => {
  await page.getByRole('link', { name: 'Open AI Advisor' }).click();
  await expect(page).toHaveURL(/\/login\?returnUrl=%2Fadvisor$/);
  await expect(page.getByRole('heading', { name: 'Sign in', exact: true })).toBeVisible();
});

test('supports keyboard access to the hero actions', async ({ page }) => {
  await page.getByRole('main').focus();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Open AI Advisor' })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Explore the portal' })).toBeFocused();
  await page.keyboard.press('Enter');
  await expect(page.getByRole('heading', { name: 'Meet your portal' })).toBeInViewport();
});

test('uses the selected language and remains readable with the saved dark theme', async ({ page }) => {
  await page.evaluate(() => {
    localStorage.setItem('bet-portal.lang', 'de');
    localStorage.setItem('app-theme', 'dark');
  });
  await page.reload();
  await expect(page.locator('body')).toHaveClass(/dark-theme/);
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Ihre Portaldaten. Ein klarerer Blick.');
  await expect(page.getByRole('link', { name: 'KI-Berater öffnen' })).toBeVisible();
  await page.getByRole('link', { name: 'Portal entdecken' }).click();
  await expect(page.getByRole('heading', { name: 'Lernen Sie Ihr Portal kennen' })).toBeInViewport();
  await page.evaluate(() => localStorage.setItem('bet-portal.lang', 'cs'));
  await page.reload();
  await expect(page.getByRole('heading', { level: 1 })).toHaveText('Data vašeho portálu. Jasnější pohled.');
});
