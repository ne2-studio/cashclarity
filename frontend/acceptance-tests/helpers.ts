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

export async function loginAsAcceptanceUser(page: Page): Promise<string> {
  await page.goto('/');
  await page.waitForURL('**/authorize**', { timeout: 15_000 });
  await page.getByRole('button', { name: 'Acceptance User' }).click();
  await page.waitForURL('/', { timeout: 15_000 });
  await expect(page.getByRole('heading', { name: 'CashClarity' })).toBeVisible();

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
  const response = await page.request.post(`${API_BASE_URL}/accounts`, {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { ...account, active: true },
  });
  expect(response.ok(), `failed to create account: ${response.status()} ${await response.text()}`).toBeTruthy();
  return await response.json() as AccountDto;
}

export async function getAccountsViaApi(page: Page, accessToken: string): Promise<AccountDto[]> {
  const response = await page.request.get(`${API_BASE_URL}/accounts`, {
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
  const response = await page.request.post(`${API_BASE_URL}/bank-movements`, {
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
