import { expect, test } from '@playwright/test';
import {
  collectUnexpectedBrowserErrors,
  createBankMovementViaApi,
  createWorkspaceFromNavbar,
  loginAsAcceptanceUser,
  openNav,
  selectWorkspaceFromNavbar,
  uniqueLabel,
} from './helpers';

test('a user without workspaces is asked to create the first one', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);
  // Act as a brand-new user so the API reports no workspaces.
  const newUserId = `new-user-${Date.now()}`;
  await page.route('**/server/**', (route) =>
    route.continue({ headers: { ...route.request().headers(), 'x-test-user-id': newUserId } }));

  await loginAsAcceptanceUser(page, { createFirstWorkspace: false });

  await expect(page.getByRole('heading', { name: 'Crea tu primer workspace' })).toBeVisible();
  await expect(page.getByTestId('workspace-switcher')).toHaveCount(0);

  const name = uniqueLabel('Primer workspace');
  await page.getByLabel('Nombre del workspace').fill(name);
  await page.getByRole('button', { name: 'Crear workspace' }).click();

  await expect(page.getByText('Visibilidad de Caja')).toBeVisible();
  await expect(page.getByTestId('workspace-switcher')).toContainText(name);

  assertNoBrowserErrors();
});

test('creating a second workspace switches to it and keeps statements separate', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);
  const accessToken = await loginAsAcceptanceUser(page);
  const firstWorkspaceName = ((await page.getByTestId('workspace-switcher').textContent()) ?? '').trim();
  const movementDescription = uniqueLabel('Movimiento workspace uno');
  await createBankMovementViaApi(page, accessToken, { date: '2026-08-05', description: movementDescription, amount: -10 });

  await page.reload();
  await openNav(page, 'Extracto Bancario');
  await expect(page.locator('tbody tr', { hasText: movementDescription })).toHaveCount(1);

  const secondName = uniqueLabel('Segundo workspace');
  await createWorkspaceFromNavbar(page, secondName);
  await expect(page.getByTestId('workspace-switcher')).toContainText(secondName);
  await expect(page.locator('tbody tr', { hasText: movementDescription })).toHaveCount(0);

  // The selection survives a reload.
  await page.reload();
  await expect(page.getByTestId('workspace-switcher')).toContainText(secondName);

  await selectWorkspaceFromNavbar(page, firstWorkspaceName);
  await expect(page.getByTestId('workspace-switcher')).toContainText(firstWorkspaceName);
  await openNav(page, 'Extracto Bancario');
  await expect(page.locator('tbody tr', { hasText: movementDescription })).toHaveCount(1);

  assertNoBrowserErrors();
});
