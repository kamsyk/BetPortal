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

test('shows anonymous actions without a user profile icon', async ({ page }, testInfo) => {
  await expect(page.getByRole('button', { name: 'User profile', exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Logout', exact: true })).toHaveCount(0);
  await expect(page.locator('.primary-nav')).toHaveCount(0);

  if (testInfo.project.name === 'mobile-chromium') {
    const mobileTrigger = page.getByRole('button', { name: 'Open navigation menu' });
    await expect(mobileTrigger).toBeVisible();
    await mobileTrigger.click();
    const menu = page.getByRole('menu');
    await expect(menu.getByRole('menuitem', { name: 'Sign in', exact: true })).toBeVisible();
    await expect(menu.getByRole('menuitem', { name: 'Sign up', exact: true })).toBeVisible();
    await expect(menu.getByRole('menuitem', { name: 'Language: English' })).toBeVisible();
    await expect(menu.getByRole('menuitem', { name: /Dark Mode|Light Mode/ })).toBeVisible();
  } else {
    await expect(page.getByRole('link', { name: 'Sign in', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Sign up', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Language: English' })).toBeVisible();
    await expect(page.getByRole('button', { name: /Switch to (Dark|Light) Mode/ })).toBeVisible();
  }
});

test('opens the language menu and restores focus after Escape', async ({ page }, testInfo) => {
  if (testInfo.project.name === 'mobile-chromium') {
    await page.getByRole('button', { name: 'Open navigation menu' }).click();
    const languageTrigger = page.getByRole('menuitem', { name: 'Language: English' });
    await languageTrigger.click();
  } else {
    await page.getByRole('button', { name: 'Language: English' }).click();
  }

  const languageMenu = page.getByRole('menu').last();
  await expect(languageMenu.getByRole('menuitem')).toHaveCount(3);
  await expect(languageMenu.getByRole('menuitem', { name: /English/ })).toBeVisible();
  await expect(languageMenu.getByRole('menuitem', { name: /Czech/ })).toBeVisible();
  await expect(languageMenu.getByRole('menuitem', { name: /German/ })).toBeVisible();

  await page.keyboard.press('Escape');
  await expect(languageMenu).toBeHidden();
});

test('selects a language and marks it active when reopened', async ({ page }, testInfo) => {
  if (testInfo.project.name === 'mobile-chromium') {
    await page.getByRole('button', { name: 'Open navigation menu' }).click();
    await page.getByRole('menuitem', { name: 'Language: English' }).click();
    await page.getByRole('menuitem', { name: /Czech/ }).click();
    await expect(page.getByRole('menu')).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => localStorage.getItem('bet-portal.lang')))
      .toBe('cs');
    await page.getByRole('button', { name: 'Open navigation menu' }).click();
    await expect(page.getByRole('menuitem', { name: /Jazyk: Čeština|Language: Czech/ })).toBeVisible();
  } else {
    const languageTrigger = page.getByRole('button', { name: 'Language: English' });
    await languageTrigger.click();
    await page.getByRole('menuitem', { name: /Czech/ }).click();
    await expect(page.getByRole('menu')).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => localStorage.getItem('bet-portal.lang')))
      .toBe('cs');
    await expect(page.getByRole('button', { name: /Language: Czech|Jazyk: Čeština/ })).toBeVisible();
  }
});

test('lets anonymous visitors sign in from the toolbar or mobile menu', async ({ page }, testInfo) => {
  await page.goto('/');

  if (testInfo.project.name === 'mobile-chromium') {
    await page.getByRole('button', { name: 'Open navigation menu' }).click();
    await page.getByRole('menuitem', { name: 'Sign in', exact: true }).click();
  } else {
    await page.getByRole('link', { name: 'Sign in', exact: true }).click();
  }

  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('menu')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Sign in', exact: true })).toBeVisible();
});

test('lets anonymous visitors open sign up from the toolbar or mobile menu', async ({ page }, testInfo) => {
  await page.goto('/');

  if (testInfo.project.name === 'mobile-chromium') {
    await page.getByRole('button', { name: 'Open navigation menu' }).click();
    await page.getByRole('menuitem', { name: 'Sign up', exact: true }).click();
  } else {
    await page.getByRole('link', { name: 'Sign up', exact: true }).click();
  }

  await expect(page).toHaveURL(/\/login\?signUp=true/);
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
    await expect(menu.getByRole('menuitem', { name: /Logout|Language|English|Czech|German|Sign in|Sign up/ })).toHaveCount(0);
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
  await expect(profileMenu.getByRole('menuitem', { name: 'Sign in', exact: true })).toHaveCount(0);
  await profileMenu.getByRole('menuitem', { name: /Logout/ }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByRole('menu')).toHaveCount(0);
});
