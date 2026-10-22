import { Account, JournalEntry, BankMovement, type Workspace } from './types';
import { getEnv } from './runtimeConfig';

const API_BASE_URL = (getEnv('VITE_API_URL') || 'http://127.0.0.1:54321/functions/v1/server').replace(/\/$/, '');

let _accessToken: string | undefined;

export function setAccessToken(token: string | undefined) {
  _accessToken = token;
}

let _workspaceId: string | undefined;

export function setActiveWorkspaceId(id: string | undefined) {
  _workspaceId = id;
}

const workspaceUrl = (): string => {
  if (!_workspaceId) throw new Error('No active workspace selected');
  return `${API_BASE_URL}/workspaces/${_workspaceId}`;
};

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
    throw new Error(error.message || error.error || `API Error: ${res.status}`);
  }
  return res.json();
};

export const api = {
  workspaces: {
    getAll: async (): Promise<Workspace[]> =>
      fetch(`${API_BASE_URL}/workspaces`, { headers: getHeaders() }).then(handleResponse),
    create: async (name: string): Promise<Workspace> =>
      fetch(`${API_BASE_URL}/workspaces`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ name }),
      }).then(handleResponse),
  },

  accounts: {
    getAll: async (): Promise<Account[]> =>
      fetch(`${workspaceUrl()}/accounts`, { headers: getHeaders() }).then(handleResponse).then(data => data.map((a: any) => new Account(a))),
    create: async (account: Omit<Account, 'id'>): Promise<Account> =>
      fetch(`${workspaceUrl()}/accounts`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(account),
      }).then(handleResponse).then(data => new Account(data)),
    update: async (id: string, updates: Partial<Account>): Promise<void> =>
      fetch(`${workspaceUrl()}/accounts/${id}`, {
        method: 'PATCH',
        headers: getHeaders(),
        body: JSON.stringify(updates),
      }).then(handleResponse),
    delete: async (id: string): Promise<void> =>
      fetch(`${workspaceUrl()}/accounts/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      }).then(handleResponse),
  },

  journalEntries: {
    getAll: async (): Promise<JournalEntry[]> =>
      fetch(`${workspaceUrl()}/journal-entries`, { headers: getHeaders() }).then(handleResponse).then(data => data.map((e: any) => new JournalEntry(e))),
    create: async (entry: Omit<JournalEntry, 'id'>): Promise<JournalEntry> =>
      fetch(`${workspaceUrl()}/journal-entries`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(entry),
      }).then(handleResponse).then(data => new JournalEntry(data)),
    update: async (id: string, updates: Partial<JournalEntry>): Promise<void> =>
      fetch(`${workspaceUrl()}/journal-entries/${id}`, {
        method: 'PATCH',
        headers: getHeaders(),
        body: JSON.stringify(updates),
      }).then(handleResponse),
    delete: async (id: string): Promise<void> =>
      fetch(`${workspaceUrl()}/journal-entries/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      }).then(handleResponse),
  },

  bankMovements: {
    getAll: async (): Promise<BankMovement[]> =>
      fetch(`${workspaceUrl()}/bank-movements`, { headers: getHeaders() }).then(handleResponse).then(data => data.map((m: any) => new BankMovement(m))),
    create: async (movement: Omit<BankMovement, 'id' | 'isIdentified'>): Promise<BankMovement> =>
      fetch(`${workspaceUrl()}/bank-movements`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(movement),
      }).then(handleResponse).then(data => new BankMovement(data)),
    update: async (id: string, updates: Partial<BankMovement>): Promise<void> =>
      fetch(`${workspaceUrl()}/bank-movements/${id}`, {
        method: 'PATCH',
        headers: getHeaders(),
        body: JSON.stringify(updates),
      }).then(handleResponse),
    delete: async (id: string): Promise<void> =>
      fetch(`${workspaceUrl()}/bank-movements/${id}`, {
        method: 'DELETE',
        headers: getHeaders()
      }).then(handleResponse),
    previewImport: async (file: File): Promise<BankMovementImportPreview> => {
      const formData = new FormData();
      formData.append('file', file);
      return fetch(`${workspaceUrl()}/bank-movements/imports/preview`, {
        method: 'POST',
        headers: getMultipartHeaders(),
        body: formData,
      }).then(handleResponse);
    },
    commitImport: async (rows: BankMovementImportRow[], duplicatePolicy: DuplicatePolicy = 'skip'): Promise<BankMovementImportCommitResult> =>
      fetch(`${workspaceUrl()}/bank-movements/imports`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify({ rows, duplicatePolicy }),
      }).then(handleResponse).then(data => ({
        ...data,
        created: data.created.map((m: any) => new BankMovement(m)),
      })),
  },
};

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
