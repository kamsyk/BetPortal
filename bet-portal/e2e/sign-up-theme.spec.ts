import { expect, test, type Locator } from '@playwright/test';

async function expectReadableText(locator: Locator): Promise<void> {
  await expect.poll(() => locator.evaluateAll(elements => {
    const canvas = document.createElement('canvas');
    canvas.width = canvas.height = 1;
    const context = canvas.getContext('2d');
    if (!context) {
      throw new Error('A canvas context is required to measure text contrast.');
    }

    const parseColor = (color: string): number[] => {
      context.clearRect(0, 0, 1, 1);
      context.fillStyle = color;
      context.fillRect(0, 0, 1, 1);
      const [red, green, blue, alpha] = context.getImageData(0, 0, 1, 1).data;
      return [red / 255, green / 255, blue / 255, alpha / 255];
    };

    function composite(foreground: number[], background: number[]): number[] {
      return foreground.slice(0, 3).map((channel, index) =>
        channel * foreground[3] + background[index] * (1 - foreground[3]));
    }

    function luminance(color: number[]): number {
      const linear = color.map(channel =>
        channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4);
      return linear[0] * 0.2126 + linear[1] * 0.7152 + linear[2] * 0.0722;
    }

    return elements.map(element => {
      const ancestors: Element[] = [];
      for (let current: Element | null = element; current; current = current.parentElement) {
        ancestors.unshift(current);
      }
      const background = ancestors.reduce((color, ancestor) =>
        composite(parseColor(getComputedStyle(ancestor).backgroundColor), color), [1, 1, 1]);
      const foreground = composite(parseColor(getComputedStyle(element).color), background);
      const values = [luminance(foreground), luminance(background)];
      return (Math.max(...values) + 0.05) / (Math.min(...values) + 0.05);
    });
  }).then(ratios => ratios.length > 0 && ratios.every(ratio => ratio >= 4.5))).toBe(true);
}

for (const theme of ['light', 'dark']) {
  test(`sign-up fields and buttons remain readable in ${theme} mode`, async ({ page }) => {
    await page.route('http://localhost:5026/api/**', route =>
      route.fulfill({ json: { isAuthenticated: false } }));
    await page.addInitScript(savedTheme => {
      localStorage.setItem('app-theme', savedTheme);
      localStorage.setItem('bet-portal.lang', 'en');
    }, theme);
    await page.goto('/sign-up');
    await expect(page.locator('body')).toHaveClass(new RegExp(`${theme}-theme`));

    const signUp = page.locator('lib-ks-sign-up');
    const labels = signUp.locator('mat-label');
    await expect(labels).toHaveCount(3);
    await expectReadableText(labels);
    await expectReadableText(signUp.getByRole('button', { name: /Sign up with/ }));
    for (const provider of ['Google', 'Facebook', 'Microsoft']) {
      const button = signUp.getByRole('button', { name: `Sign up with ${provider}`, exact: true });
      const icon = button.locator('svg');
      await expect(icon).toBeVisible();
      await expect(icon).toHaveAttribute('aria-hidden', 'true');
      await expect(icon).toHaveAttribute('focusable', 'false');
      expect(await icon.evaluate(element => {
        const bounds = element.getBoundingClientRect();
        return bounds.width >= 20 && bounds.height >= 20;
      })).toBe(true);
      expect(await icon.locator('path').count()).toBeGreaterThan(0);
    }

    const username = signUp.getByRole('textbox', { name: 'Username', exact: true });
    const email = signUp.getByRole('textbox', { name: 'Email', exact: true });
    await username.fill('contrast-test-user');
    await expectReadableText(username);
    await expectReadableText(labels);
    await email.fill('invalid-email');
    await signUp.getByRole('textbox', { name: 'Password', exact: true }).fill('Test-password-42');
    await expect(signUp.locator('mat-error')).toBeVisible();
    if (theme === 'dark') {
      await expectReadableText(signUp.locator('mat-error'));
      await expectReadableText(labels);
    }
    await email.fill('contrast-test@example.test');
    await username.focus();
    await expectReadableText(signUp.locator('input'));
    const submit = signUp.getByRole('button', { name: 'Sign Up', exact: true });
    await expect(submit).toBeEnabled();
    await expectReadableText(submit);
  });
}
