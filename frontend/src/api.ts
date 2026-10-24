import { Account, JournalEntry, BankMovement } from './types';
import { getEnv } from './runtimeConfig';

const API_BASE_URL = (getEnv('VITE_API_URL') || 'http://127.0.0.1:54321/functions/v1/server').replace(/\/$/, '');

let _accessToken: string | undefined;

export function setAccessToken(token: string | undefined) {
  _accessToken = token;
}

const getHeaders = (): Record<string, string> => ({
  'Content-Type': 'application/json',
  ...(typeof _accessToken === 'string' ? { 'Authorization': `Bearer ${_accessToken}` } : {}),
});

const getMultipartHeaders = (): Record<string, string> => ({
  ...(typeof _accessToken === 'string' ? { 'Authorization': `Bearer ${_accessToken}` } : {}),
});

const handleResponse = async (res: Response) => {
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Unknown error' }));
    throw new Error(error.error || error.message || `API Error: ${res.status}`);
  }
  return res.json();
};

export const api = {
  accounts: {
    getAll: async (): Promise<Account[]> =>
      fetch(`${API_BASE_URL}/accounts`, { headers: getHeaders() }).then(handleResponse).then(data => data.map((a: any) => new Account(a))),
    create: async (account: Omit<Account, 'id'>): Promise<Account> =>
      fetch(`${API_BASE_URL}/accounts`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(account),
      }).then(handleResponse).then(data => new Account(data)),
    update: async (id: string, updates: Partial<Account>): Promise<void> =>
      fetch(`${API_BASE_URL}/accounts/${id}`, {
        method: 'PATCH',
        headers: getHeaders(),
        body: JSON.stringify(updates),
      }).then(handleResponse),
    delete: async (id: string): Promise<void> =>
      fetch(`${API_BASE_URL}/accounts/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      }).then(handleResponse),
  },

  journalEntries: {
    getAll: async (): Promise<JournalEntry[]> =>
      fetch(`${API_BASE_URL}/journal-entries`, { headers: getHeaders() }).then(handleResponse).then(data => data.map((e: any) => new JournalEntry(e))),
    create: async (entry: Omit<JournalEntry, 'id'>): Promise<JournalEntry> =>
      fetch(`${API_BASE_URL}/journal-entries`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(entry),
      }).then(handleResponse).then(data => new JournalEntry(data)),
    update: async (id: string, updates: Partial<JournalEntry>): Promise<void> =>
      fetch(`${API_BASE_URL}/journal-entries/${id}`, {
        method: 'PATCH',
        headers: getHeaders(),
        body: JSON.stringify(updates),
      }).then(handleResponse),
    delete: async (id: string): Promise<void> =>
      fetch(`${API_BASE_URL}/journal-entries/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      }).then(handleResponse),
  },

  bankMovements: {
    getAll: async (): Promise<BankMovement[]> =>
      fetch(`${API_BASE_URL}/bank-movements`, { headers: getHeaders() }).then(handleResponse).then(data => data.map((m: any) => new BankMovement(m))),
    create: async (movement: Omit<BankMovement, 'id' | 'isIdentified'>): Promise<BankMovement> =>
      fetch(`${API_BASE_URL}/bank-movements`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(movement),
      }).then(handleResponse).then(data => new BankMovement(data)),
    update: async (id: string, updates: Partial<BankMovement>): Promise<void> =>
      fetch(`${API_BASE_URL}/bank-movements/${id}`, {
        method: 'PATCH',
        headers: getHeaders(),
        body: JSON.stringify(updates),
      }).then(handleResponse),
    delete: async (id: string): Promise<void> =>
      fetch(`${API_BASE_URL}/bank-movements/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      }).then(handleResponse),
    previewImport: async (file: File): Promise<BankMovementImportPreview> => {
      const formData = new FormData();
      formData.append('file', file);
      return fetch(`${API_BASE_URL}/bank-movements/imports/preview`, {
        method: 'POST',
        headers: getMultipartHeaders(),
        body: formData,
      }).then(handleResponse);
    },
    commitImport: async (rows: BankMovementImportRow[], duplicatePolicy: DuplicatePolicy = 'skip'): Promise<BankMovementImportCommitResult> =>
      fetch(`${API_BASE_URL}/bank-movements/imports`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ rows, duplicatePolicy }),
      }).then(handleResponse).then(data => ({
        ...data,
        created: data.created.map((m: any) => new BankMovement(m)),
      })),
  },

  bankLink: {
    get: async (): Promise<BankLink> =>
      fetch(`${API_BASE_URL}/bank-link`, { headers: getHeaders() }).then(handleResponse),
    getAspsps: async (country = 'ES'): Promise<BankAspsp[]> =>
      fetch(`${API_BASE_URL}/bank-link/aspsps?country=${encodeURIComponent(country)}`, { headers: getHeaders() }).then(handleResponse),
    startAuthorization: async (request: BankAuthorizationStart): Promise<{ url: string }> =>
      fetch(`${API_BASE_URL}/bank-link/authorizations`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(request),
      }).then(handleResponse),
    completeAuthorization: async (code: string, state: string): Promise<BankLink> =>
      fetch(`${API_BASE_URL}/bank-link/sessions`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ code, state }),
      }).then(handleResponse),
    selectAccount: async (accountUid: string): Promise<BankLink> =>
      fetch(`${API_BASE_URL}/bank-link/account`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ accountUid }),
      }).then(handleResponse),
    sync: async (): Promise<BankSyncResult> =>
      fetch(`${API_BASE_URL}/bank-link/sync`, {
        method: 'POST',
        headers: getHeaders(),
      }).then(handleResponse).then(data => ({
        ...data,
        created: data.created.map((m: any) => new BankMovement(m)),
      })),
    unlink: async (): Promise<void> =>
      fetch(`${API_BASE_URL}/bank-link`, {
        method: 'DELETE',
        headers: getHeaders(),
      }).then(handleResponse),
  },
};

export type BankLinkStatus = 'none' | 'pending' | 'awaiting-account' | 'active' | 'expired';
export type PsuType = 'personal' | 'business';

export interface BankAccountOption {
  uid: string;
  iban?: string | null;
  name?: string | null;
  currency?: string | null;
}

export interface BankLink {
  configured: boolean;
  status: BankLinkStatus;
  aspspName?: string | null;
  aspspCountry?: string | null;
  psuType?: PsuType | null;
  iban?: string | null;
  accountName?: string | null;
  currency?: string | null;
  validUntil?: string | null;
  syncFrom?: string | null;
  lastSyncedAt?: string | null;
  accountOptions?: BankAccountOption[] | null;
}

export interface BankAspsp {
  name: string;
  country: string;
  psuTypes: PsuType[];
  maximumConsentValidityDays?: number | null;
  logo?: string | null;
}

export interface BankAuthorizationStart {
  aspspName: string;
  aspspCountry: string;
  psuType: PsuType;
  syncFrom?: string | null;
}

export interface BankSyncResult {
  created: BankMovement[];
  skippedExisting: number;
  possibleDuplicates: { movementId: string; existingMovementId: string }[];
  dateFrom: string;
  dateTo: string;
  balance?: { amount: number; currency?: string | null; type?: string | null; referenceDate?: string | null } | null;
  link: BankLink;
}

export type BankMovementImportRowStatus = 'valid' | 'duplicate' | 'warning' | 'invalid';
export type DuplicatePolicy = 'skip' | 'import-anyway';

export interface BankMovementImportRow {
  rowNumber: number;
  date?: string | null;
  description?: string | null;
  amount?: number | null;
  status: BankMovementImportRowStatus;
  duplicateOfBankMovementId?: string | null;
  errors: string[];
  headers?: string[] | null;
  selected?: boolean;
}

export interface BankMovementImportPreview {
  rows: BankMovementImportRow[];
  summary: {
    totalRows: number;
    valid: number;
    duplicates: number;
    invalid: number;
    warnings: number;
  };
}

export interface BankMovementImportCommitResult {
  created: BankMovement[];
  skippedDuplicates: number;
  failed: { rowNumber: number; error: string }[];
}
