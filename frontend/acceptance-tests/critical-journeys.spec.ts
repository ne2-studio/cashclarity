import { expect, test } from '@playwright/test';
import {
  cashText,
  collectUnexpectedBrowserErrors,
  createAccountViaApi,
  createBankMovementViaApi,
  loginAsAcceptanceUser,
  openNav,
  uniqueLabel,
} from './helpers';

test('login and primary navigation reach every workspace area', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);

  await loginAsAcceptanceUser(page);

  await expect(page.getByText('Visibilidad de Caja')).toBeVisible();

  await openNav(page, 'Extracto Bancario');
  await expect(page.getByText('Extracto de Movimientos Bancarios')).toBeVisible();

  await openNav(page, 'Espacios');
  await expect(page.getByRole('heading', { name: 'Espacios de Reserva' })).toBeVisible();

  await openNav(page, 'Entidades');
  await expect(page.getByRole('heading', { name: 'Entidades // Clientes, Proveedores y Otros' })).toBeVisible();

  await openNav(page, 'Libro Diario');
  await expect(page.getByRole('heading', { name: 'Libro Diario // Registro Contable' })).toBeVisible();

  await openNav(page, 'Plan Contable');
  await expect(page.getByRole('heading', { name: 'Plan Contable' })).toBeVisible();

  assertNoBrowserErrors();
});

test('create accounts from Spaces, Entities and Chart of Accounts', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);
  await loginAsAcceptanceUser(page);

  const spaceName = uniqueLabel('Reserva impuestos');
  const entityName = uniqueLabel('Cliente aceptación');
  const extraSpaceName = uniqueLabel('Fondo operativo');

  await openNav(page, 'Espacios');
  await page.getByRole('button', { name: 'Nuevo Espacio' }).click();
  await page.getByPlaceholder('Ej: 5722').fill('57' + Date.now().toString().slice(-2));
  await page.getByPlaceholder('Ej: Reserva IVA').fill(spaceName);
  await page.getByRole('button', { name: 'Crear Espacio' }).click();
  await expect(page.getByRole('button', { name: new RegExp(spaceName) })).toBeVisible();

  await openNav(page, 'Entidades');
  await page.getByRole('button', { name: 'Nueva Entidad' }).click();
  await page.getByPlaceholder('Ej: 4300').fill('43' + Date.now().toString().slice(-2));
  await page.getByPlaceholder('Ej: Cliente A').fill(entityName);
  await page.getByRole('button', { name: 'Crear Entidad' }).click();
  await expect(page.getByRole('button', { name: new RegExp(entityName) })).toBeVisible();

  await openNav(page, 'Plan Contable');
  await page.getByRole('button', { name: 'Nueva Cuenta' }).click();
  await page.getByPlaceholder('Ej: 5721').fill('58' + Date.now().toString().slice(-2));
  await page.getByPlaceholder('Ej: Banco Sabadell').fill(extraSpaceName);
  await page.locator('select').selectOption('space');
  await page.getByRole('button', { name: 'Crear Cuenta' }).click();
  await page.getByPlaceholder('Buscar por nombre o código...').fill(extraSpaceName);
  await expect(page.locator('tbody tr', { hasText: extraSpaceName })).toContainText('ESPACIO');

  assertNoBrowserErrors();
});

test('bank income can be identified, reserved and reflected in dashboard, spaces, entities and journal', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);
  const accessToken = await loginAsAcceptanceUser(page);
  const suffix = Date.now().toString().slice(-6);
  const space = await createAccountViaApi(page, accessToken, {
    code: `61${suffix.slice(-2)}`,
    name: uniqueLabel('Reserva IVA'),
    type: 'space',
  });
  const entity = await createAccountViaApi(page, accessToken, {
    code: `44${suffix.slice(-2)}`,
    name: uniqueLabel('Cliente Norte'),
    type: 'entity',
  });
  const incomeDescription = uniqueLabel('Factura cobrada');

  await page.reload();
  await openNav(page, 'Extracto Bancario');
  await page.getByRole('button', { name: 'Nuevo Movimiento' }).click();
  await page.locator('input[type="date"]').fill('2026-08-01');
  await page.getByPlaceholder('Ej: Transferencia Recibida').fill(incomeDescription);
  await page.getByPlaceholder('0.00').fill('1200');
  await page.getByRole('button', { name: 'Guardar Movimiento' }).click();

  const incomeRow = page.locator('tbody tr', { hasText: incomeDescription });
  await expect(incomeRow).toContainText(cashText(1200));
  await incomeRow.getByTitle('Haga clic para identificar entidad').click();
  await page.locator('select').selectOption(entity.id);
  await page.getByRole('button', { name: 'Identificar', exact: true }).click();
  await expect(incomeRow).toContainText(entity.name);

  await incomeRow.getByTitle('Reservar Fondos').click();
  await page.getByRole('button', { name: '+ Añadir Reserva' }).click();
  await page.locator('select').last().selectOption(space.id);
  await page.locator('input[type="number"]').last().fill('300');
  await page.getByRole('button', { name: 'Guardar Reservas' }).click();
  await expect(page.getByRole('heading', { name: 'Reservar Fondos' })).toBeHidden();

  await openNav(page, 'Dashboard');
  await expect(page.locator('.financial-card', { hasText: 'Saldo Bancario Real' })).toContainText(cashText(1200));
  await expect(page.locator('.financial-card', { hasText: 'Saldo Comprometido' })).toContainText(cashText(300));
  await expect(page.locator('.financial-card', { hasText: 'Saldo Disponible' })).toContainText(cashText(900));

  await openNav(page, 'Espacios');
  await page.getByRole('button', { name: new RegExp(space.name) }).click();
  await expect(page.locator('tbody tr', { hasText: incomeDescription })).toContainText(cashText(300));

  await openNav(page, 'Entidades');
  await page.getByPlaceholder('Buscar entidad...').fill(entity.name);
  await page.getByRole('button', { name: new RegExp(entity.name) }).click();
  await expect(page.locator('tbody tr', { hasText: incomeDescription })).toContainText(cashText(-1200));

  await openNav(page, 'Libro Diario');
  await page.getByPlaceholder('Buscar por concepto o cuenta...').fill(incomeDescription);
  await expect(page.locator('tbody tr', { hasText: 'Cuenta Principal' }).filter({ hasText: cashText(1200) })).toHaveCount(1);
  await expect(page.locator('tbody tr', { hasText: space.name }).filter({ hasText: cashText(300) })).toHaveCount(1);
  await expect(page.locator('tbody tr', { hasText: entity.name }).filter({ hasText: cashText(1200) })).toHaveCount(1);

  assertNoBrowserErrors();
});

test('expense can be paid from a reserved space', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);
  const accessToken = await loginAsAcceptanceUser(page);
  const suffix = Date.now().toString().slice(-6);
  const space = await createAccountViaApi(page, accessToken, {
    code: `62${suffix.slice(-2)}`,
    name: uniqueLabel('Reserva nóminas'),
    type: 'space',
  });
  const expenseDescription = uniqueLabel('Pago proveedor');
  await createBankMovementViaApi(page, accessToken, {
    date: '2026-08-05',
    description: expenseDescription,
    amount: -125,
  });

  await page.reload();
  await openNav(page, 'Extracto Bancario');
  const expenseRow = page.locator('tbody tr', { hasText: expenseDescription });
  await expect(expenseRow).toContainText(cashText(-125));

  await expenseRow.getByTitle('Pagar desde Espacio').click();
  await page.locator('select').selectOption(space.id);
  await page.getByRole('button', { name: 'Confirmar Pago' }).click();
  await expect(page.getByRole('heading', { name: 'Pagar desde Espacio' })).toBeHidden();

  await openNav(page, 'Libro Diario');
  await page.getByPlaceholder('Buscar por concepto o cuenta...').fill(expenseDescription);
  await expect(page.locator('tbody tr', { hasText: space.name })).toContainText(cashText(125));
  await expect(page.locator('tbody tr', { hasText: 'Sin categorizar' })).toContainText(cashText(125));

  assertNoBrowserErrors();
});

test('CSV import previews and imports bank movements', async ({ page }, testInfo) => {
  const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);
  await loginAsAcceptanceUser(page);
  const importedDescription = uniqueLabel('Ingreso CSV');

  await openNav(page, 'Extracto Bancario');
  await page.getByRole('button', { name: 'Importar CSV' }).click();
  await page.locator('input[type="file"]').setInputFiles({
    name: 'movimientos.csv',
    mimeType: 'text/csv',
    buffer: Buffer.from(`fecha;Concepto;cantidad\n2026-08-10;${importedDescription};42,50\n`),
  });
  await expect(page.getByText('Se han detectado 1 movimientos válidos.')).toBeVisible();
  await expect(page.locator('tbody tr', { hasText: importedDescription })).toContainText(cashText(42.5));

  await page.getByRole('button', { name: 'Importar 1 Movimientos' }).click();
  await expect(page.getByRole('heading', { name: 'Importar Movimientos CSV' })).toBeHidden();
  await expect(page.locator('tbody tr', { hasText: importedDescription })).toContainText(cashText(42.5));

  assertNoBrowserErrors();
});
