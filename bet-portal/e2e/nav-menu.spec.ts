import { expect, test } from '@playwright/test';

const apiOrigin = 'http://localhost:5026';

test.beforeEach(async ({ page }) => {
  await page.route(`${apiOrigin}/api/**`, async (route) => {
    const { pathname } = new URL(route.request().url());

    if (pathname === '/api/auth/check') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ isAuthenticated: false }),
      });
      return;
    }

    await route.fulfill({
      status: 501,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'Unexpected API request in mocked UI test' }),
    });
  });
});

test.beforeEach(async ({ page }) => {
  await page.goto('/login');
  await expect(page.locator('app-nav-menu')).toBeVisible();
});

test('shows the profile icon without language or logout in the main toolbar', async ({ page }) => {
  const mobileTrigger = page.getByRole('button', { name: 'Open navigation menu' });
  const languageTrigger = page.getByRole('button', { name: 'Language: English' });

  await expect(mobileTrigger).toHaveCount(0);
  await expect(languageTrigger).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Logout', exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'User profile', exact: true })).toBeVisible();

  await expect(page.locator('.primary-nav')).toHaveCount(0);
});

test('opens the profile submenu and restores focus after Escape', async ({ page }) => {
  const trigger = page.getByRole('button', { name: 'User profile', exact: true });

  await expect(trigger).toHaveAttribute('aria-expanded', 'false');
  await trigger.click();

  const menu = page.getByRole('menu');
  await expect(menu).toBeVisible();
  await expect(trigger).toHaveAttribute('aria-expanded', 'true');
  await expect(menu.getByRole('menuitem')).toHaveCount(1);
  const languageTrigger = menu.getByRole('menuitem', { name: 'Language: English' });
  await languageTrigger.click();
  const languageMenuId = await languageTrigger.getAttribute('aria-controls');
  expect(languageMenuId).toBeTruthy();
  const languageMenu = page.locator(`[id="${languageMenuId}"]`);
  await expect(languageMenu.getByRole('menuitem')).toHaveCount(3);
  await expect(languageMenu.getByRole('menuitem', { name: /English/ })).toBeVisible();
  await expect(languageMenu.getByRole('menuitem', { name: /Czech/ })).toBeVisible();
  await expect(languageMenu.getByRole('menuitem', { name: /German/ })).toBeVisible();

  await page.keyboard.press('Escape');
  await expect(languageMenu).toBeHidden();
  await expect(languageTrigger).toBeFocused();
  await page.keyboard.press('Escape');

  await expect(menu).toBeHidden();
  await expect(trigger).toHaveAttribute('aria-expanded', 'false');
  await expect(trigger).toBeFocused();
});

test('selects a language from the profile submenu and marks it active when reopened', async ({ page }) => {
  const languageTrigger = page.locator('.language-button');
  const trigger = page.locator('.profile-button');

  await trigger.click();
  const initialLanguageLabel = await languageTrigger.getAttribute('aria-label');
  await languageTrigger.click();
  await page.getByRole('menuitem', { name: /Czech/ }).click();

  await expect(page.getByRole('menu')).toHaveCount(0);
  await expect.poll(() => page.evaluate(() => localStorage.getItem('bet-portal.lang')))
    .toBe('cs');

  await trigger.click();
  await expect(languageTrigger).not.toHaveAttribute('aria-label', initialLanguageLabel!);
  await expect(trigger).toHaveAttribute('aria-label', 'Uživatelský profil');
  await languageTrigger.click();
  await expect(page.locator('[role="menuitem"].active-language')).toHaveCount(1);
});

test('shows authenticated navigation, marks the active route, and navigates home', async ({ page }, testInfo) => {
  let signedIn = true;
  await page.unrouteAll();
  await page.route(`${apiOrigin}/api/**`, async (route) => {
    const { pathname } = new URL(route.request().url());

    if (pathname === '/api/auth/check') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ isAuthenticated: signedIn, username: 'synthetic-user' }),
      });
      return;
    }

    if (pathname === '/api/auth/logout') {
      signedIn = false;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true, message: 'Logged out' }),
      });
      return;
    }

    if (pathname === '/api/auth/me') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 1,
          username: 'synthetic-user',
          fullName: 'Synthetic User',
        }),
      });
      return;
    }

    await route.fulfill({
      status: 501,
      contentType: 'application/json',
      body: JSON.stringify({ message: 'Unexpected API request in mocked UI test' }),
    });
  });

  await page.goto('/advisor');

  if (testInfo.project.name === 'mobile-chromium') {
    const trigger = page.locator('.mobile-menu-button');
    await trigger.click();

    const menu = page.getByRole('menu');
    await expect(menu.getByRole('menuitem', { name: /Logout|Language|English|Czech|German/ })).toHaveCount(0);
    const advisorLink = menu.getByRole('menuitem', { name: /Advisor/ });
    await expect(advisorLink).toHaveAttribute('aria-current', 'page');
    await expect(menu.getByRole('menuitem', { name: /Home/ })).not.toHaveAttribute('aria-current', 'page');
    await menu.getByRole('menuitem', { name: /Home/ }).click();
    await expect(page).toHaveURL(/\/$/);
    await expect(menu).toBeHidden();
  } else {
    await expect(page.locator('.primary-nav').getByRole('link', { name: /Advisor/ }))
      .toHaveAttribute('aria-current', 'page');
    await page.locator('.brand-link').click();
    await expect(page).toHaveURL(/\/$/);
  }

  await page.getByRole('button', { name: 'User profile', exact: true }).click();
  const profileMenu = page.getByRole('menu');
  await expect(profileMenu.getByRole('menuitem')).toHaveCount(2);
  await expect(profileMenu.getByRole('menuitem', { name: 'Language: English' })).toBeVisible();
  await expect(profileMenu.getByRole('menuitem', { name: /Logout/ })).toBeVisible();
  await profileMenu.getByRole('menuitem', { name: /Logout/ }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('menu')).toHaveCount(0);
});
