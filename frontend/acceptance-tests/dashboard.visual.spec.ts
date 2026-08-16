import { expect, test } from '@playwright/test';

test('dashboard visual baseline', async ({ page }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'CashClarity' })).toBeVisible();
  await expect(page.getByText('Visibilidad de Caja')).toBeVisible();
  await expect(page).toHaveScreenshot('dashboard.png', {
    fullPage: true,
    animations: 'disabled',
  });
});
