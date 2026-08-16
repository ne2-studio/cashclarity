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
});
