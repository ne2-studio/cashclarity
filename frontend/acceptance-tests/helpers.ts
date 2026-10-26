import type { Page, TestInfo } from '@playwright/test';
import { expect } from '@playwright/test';

export const API_BASE_URL = 'http://localhost:5051/server';

type AccountType = 'main' | 'space' | 'entity' | 'uncategorized';

export type AccountDto = {
  id: string;
  code: string;
  name: string;
  type: AccountType;
  isSystem?: boolean;
  active: boolean;
};

export type BankMovementDto = {
  id: string;
  date: string;
  description: string;
  amount: number;
  isIdentified: boolean;
  entityId?: string;
  journalEntryId?: string;
};

export const DEFAULT_WORKSPACE_NAME = 'Principal';
export const ACTIVE_WORKSPACE_STORAGE_KEY = 'cashclarity.activeWorkspaceId';

export async function loginAsAcceptanceUser(
  page: Page,
  options: { createFirstWorkspace?: boolean } = {},
): Promise<string> {
  const { createFirstWorkspace = true } = options;
  await page.goto('/');
  await page.waitForURL('**/authorize**', { timeout: 15_000 });
  await page.getByRole('button', { name: 'Acceptance User' }).click();
  await page.waitForURL('/', { timeout: 15_000 });

  // A user without workspaces lands on the "create your first workspace" screen.
  const firstWorkspaceScreen = page.getByRole('heading', { name: 'Crea tu primer workspace' });
  await expect(firstWorkspaceScreen.or(page.getByRole('heading', { name: 'CashClarity' }))).toBeVisible();
  if (createFirstWorkspace && (await firstWorkspaceScreen.isVisible())) {
    await page.getByLabel('Nombre del workspace').fill(DEFAULT_WORKSPACE_NAME);
    await page.getByRole('button', { name: 'Crear workspace' }).click();
  }
  if (createFirstWorkspace) {
    await expect(page.getByRole('heading', { name: 'CashClarity' })).toBeVisible();
    await expect(page.getByTestId('workspace-switcher')).toBeVisible();
  }

  const accessToken = await page.evaluate(() => {
    const stores = [localStorage, sessionStorage];
    for (const store of stores) {
      for (let i = 0; i < store.length; i += 1) {
        const key = store.key(i);
        if (!key?.startsWith('oidc.user:')) continue;
        const raw = store.getItem(key);
        if (!raw) continue;
        const parsed = JSON.parse(raw);
        if (parsed.access_token) return parsed.access_token;
      }
    }
    return null;
  });
  return accessToken ?? 'acceptance-token';
}

export async function getActiveWorkspaceId(page: Page): Promise<string> {
  const id = await page.evaluate((key) => localStorage.getItem(key), ACTIVE_WORKSPACE_STORAGE_KEY);
  expect(id, 'an active workspace must be selected').toBeTruthy();
  return id as string;
}

async function workspaceApiUrl(page: Page): Promise<string> {
  return `${API_BASE_URL}/workspaces/${await getActiveWorkspaceId(page)}`;
}

export async function selectWorkspaceFromNavbar(page: Page, name: string) {
  await page.getByTestId('workspace-switcher').click();
  await page.getByRole('menuitemradio', { name }).click();
}

export async function createWorkspaceFromNavbar(page: Page, name: string) {
  await page.getByTestId('workspace-switcher').click();
  await page.getByRole('menuitem', { name: 'Crear nuevo workspace' }).click();
  await page.getByLabel('Nombre del workspace').fill(name);
  await page.getByRole('button', { name: 'Crear workspace' }).click();
  await expect(page.getByRole('heading', { name: 'Crear nuevo workspace' })).toBeHidden();
}

export function collectUnexpectedBrowserErrors(page: Page, testInfo: TestInfo) {
  const pageErrors: string[] = [];
  const failedResponses: string[] = [];

  page.on('pageerror', (err) => pageErrors.push(err.message));
  page.on('response', (res) => {
    if (res.status() >= 400) {
      failedResponses.push(`${res.status()} ${res.url()}`);
    }
  });

  return () => {
    expect(pageErrors, `page errors in ${testInfo.title}`).toEqual([]);
    expect(failedResponses, `failed responses in ${testInfo.title}`).toEqual([]);
  };
}

export function uniqueLabel(prefix: string): string {
  return `${prefix} ${Date.now()} ${Math.random().toString(16).slice(2, 8)}`;
}

export async function createAccountViaApi(
  page: Page,
  accessToken: string,
  account: { code: string; name: string; type: 'space' | 'entity' },
): Promise<AccountDto> {
  const response = await page.request.post(`${await workspaceApiUrl(page)}/accounts`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { ...account, active: true },
  });
  expect(response.ok(), `failed to create account: ${response.status()} ${await response.text()}`).toBeTruthy();
  return await response.json() as AccountDto;
}

export async function getAccountsViaApi(page: Page, accessToken: string): Promise<AccountDto[]> {
  const response = await page.request.get(`${await workspaceApiUrl(page)}/accounts`, {
    headers: { Authorization: `Bearer ${accessToken}` },
  });
  expect(response.ok(), `failed to list accounts: ${response.status()} ${await response.text()}`).toBeTruthy();
  return await response.json() as AccountDto[];
}

export async function createBankMovementViaApi(
  page: Page,
  accessToken: string,
  movement: { date: string; description: string; amount: number },
): Promise<BankMovementDto> {
  const response = await page.request.post(`${await workspaceApiUrl(page)}/bank-movements`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: movement,
  });
  expect(response.ok(), `failed to create bank movement: ${response.status()} ${await response.text()}`).toBeTruthy();
  return await response.json() as BankMovementDto;
}

export function cashText(amount: number): string {
  return new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(amount);
}

export async function openNav(page: Page, name: string) {
  await page.getByRole('link', { name }).click();
}
