import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, setAccessToken } from './api';

describe('api client', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    setAccessToken(undefined);
  });

  it('maps account responses and sends bearer token', async () => {
    setAccessToken('token-1');
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify([
      { id: 'a1', code: '0001', name: 'Cuenta Principal', type: 'main', active: true, isSystem: true },
    ])));

    const accounts = await api.accounts.getAll();

    expect(accounts[0].name).toBe('Cuenta Principal');
    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:54321/functions/v1/server/accounts',
      { headers: { 'Content-Type': 'application/json', Authorization: 'Bearer token-1' } },
    );
  });

  it('throws API status when response body is not json', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response('fail', { status: 500 }));

    await expect(api.accounts.getAll()).rejects.toThrow('Unknown error');
  });

  it('previews bank movement imports with multipart form data', async () => {
    setAccessToken('token-1');
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({
      rows: [],
      summary: { totalRows: 0, valid: 0, duplicates: 0, invalid: 0, warnings: 0 },
    })));

    await api.bankMovements.previewImport(new File(['fecha;Concepto;cantidad'], 'movimientos.csv', { type: 'text/csv' }));

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:54321/functions/v1/server/bank-movements/imports/preview',
      expect.objectContaining({
        method: 'POST',
        headers: { Authorization: 'Bearer token-1' },
        body: expect.any(FormData),
      }),
    );
  });

  it('commits bank movement imports and maps created movements', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({
      created: [{ id: 'm1', date: '2026-01-02', description: 'Nomina', amount: 1000, isIdentified: false }],
      skippedDuplicates: 0,
      failed: [],
    })));

    const result = await api.bankMovements.commitImport([
      { rowNumber: 2, date: '2026-01-02', description: 'Nomina', amount: 1000, status: 'valid', errors: [] },
    ]);

    expect(result.created[0].description).toBe('Nomina');
    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:54321/functions/v1/server/bank-movements/imports',
      expect.objectContaining({
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
      }),
    );
  });

  it('surfaces backend error messages from the error field', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ error: 'El consentimiento bancario ha caducado.' }), { status: 400 }));

    await expect(api.bankLink.sync()).rejects.toThrow('El consentimiento bancario ha caducado.');
  });

  it('syncs the bank link and maps created movements', async () => {
    setAccessToken('token-1');
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({
      created: [{ id: 'm1', date: '2026-10-20', description: 'Nomina', amount: 1500, isIdentified: false, externalId: 'ref:1' }],
      skippedExisting: 2,
      possibleDuplicates: [],
      dateFrom: '2026-10-01',
      dateTo: '2026-10-24',
      balance: null,
      link: { configured: true, status: 'active' },
    })));

    const result = await api.bankLink.sync();

    expect(result.created[0].externalId).toBe('ref:1');
    expect(result.skippedExisting).toBe(2);
    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:54321/functions/v1/server/bank-link/sync',
      { method: 'POST', headers: { 'Content-Type': 'application/json', Authorization: 'Bearer token-1' } },
    );
  });

  it('completes the bank authorization with code and state', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(JSON.stringify({ configured: true, status: 'active' })));

    await api.bankLink.completeAuthorization('code-1', 'state-1');

    expect(fetchMock).toHaveBeenCalledWith(
      'http://127.0.0.1:54321/functions/v1/server/bank-link/sessions',
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ code: 'code-1', state: 'state-1' }) }),
    );
  });
});
