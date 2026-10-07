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

test('shows the navigation controls appropriate to the viewport', async ({ page }, testInfo) => {
  const mobile = testInfo.project.name === 'mobile-chromium';
  const mobileTrigger = page.getByRole('button', { name: 'Open navigation menu' });
  const languageTrigger = page.getByRole('button', { name: 'Language: English' });

  if (mobile) {
    await expect(mobileTrigger).toBeVisible();
    await expect(languageTrigger).toBeHidden();
  } else {
    await expect(mobileTrigger).toBeHidden();
    await expect(languageTrigger).toBeVisible();
  }

  await expect(page.locator('.primary-nav')).toHaveCount(0);
});

test('opens the language menu and restores focus after Escape', async ({ page }, testInfo) => {
  const mobile = testInfo.project.name === 'mobile-chromium';
  const trigger = mobile
    ? page.getByRole('button', { name: 'Open navigation menu' })
    : page.getByRole('button', { name: 'Language: English' });

  await expect(trigger).toHaveAttribute('aria-expanded', 'false');
  await trigger.click();

  const menu = page.getByRole('menu');
  await expect(menu).toBeVisible();
  await expect(trigger).toHaveAttribute('aria-expanded', 'true');
  await expect(menu.getByRole('menuitem')).toHaveCount(3);
  await expect(menu.getByRole('menuitem', { name: /English/ })).toBeVisible();
  await expect(menu.getByRole('menuitem', { name: /Czech/ })).toBeVisible();
  await expect(menu.getByRole('menuitem', { name: /German/ })).toBeVisible();

  await page.keyboard.press('Escape');

  await expect(menu).toBeHidden();
  await expect(trigger).toHaveAttribute('aria-expanded', 'false');
  await expect(trigger).toBeFocused();
});

test('selects a language, closes the menu, and marks it active when reopened', async ({ page }, testInfo) => {
  const mobile = testInfo.project.name === 'mobile-chromium';
  const languageTrigger = page.locator('.language-button');
  const trigger = mobile
    ? page.locator('.mobile-menu-button')
    : languageTrigger;
  const initialLanguageLabel = await languageTrigger.getAttribute('aria-label');

  await trigger.click();
  await page.getByRole('menuitem', { name: /Czech/ }).click();

  const menu = page.getByRole('menu');
  await expect(menu).toBeHidden();
  await expect.poll(() => page.evaluate(() => localStorage.getItem('bet-portal.lang')))
    .toBe('cs');

  await expect.poll(() => languageTrigger.getAttribute('aria-label'))
    .not.toBe(initialLanguageLabel);
  await trigger.click();
  await expect(page.locator('[role="menuitem"].active-language')).toHaveCount(1);
});

test('shows authenticated navigation, marks the active route, and navigates home', async ({ page }, testInfo) => {
  await page.unrouteAll();
  await page.route(`${apiOrigin}/api/**`, async (route) => {
    const { pathname } = new URL(route.request().url());

    if (pathname === '/api/auth/check') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ isAuthenticated: true, username: 'synthetic-user' }),
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
});
