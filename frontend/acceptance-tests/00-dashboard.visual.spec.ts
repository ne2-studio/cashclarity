import { expect, test } from '@playwright/test';
import { collectUnexpectedBrowserErrors, loginAsAcceptanceUser } from './helpers';

test('dashboard visual baseline', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);

  await loginAsAcceptanceUser(page);

  await expect(page.getByRole('heading', { name: 'CashClarity' })).toBeVisible();
  await expect(page.getByText('Visibilidad de Caja')).toBeVisible();
  await expect(page).toHaveScreenshot('dashboard.png', {
    fullPage: true,
    animations: 'disabled',
  });
  assertNoBrowserErrors();
});
