import { useCallback, useEffect, useState } from 'react';
import { api, type BankAuthorizationStart, type BankLink, type BankSyncResult } from '../api';
import { useFinanceStore } from '../store/useFinanceStore';
import { useWorkspaceStore } from '../store/useWorkspaceStore';

/** Workspace que inició la vinculación; el callback del banco lo usa para completarla. */
export const BANK_LINK_WORKSPACE_STORAGE_KEY = 'cashclarity.bankLinkWorkspaceId';

export function useBankLink() {
  const syncBankLink = useFinanceStore((state) => state.syncBankLink);
  const activeWorkspaceId = useWorkspaceStore((state) => state.activeWorkspaceId);
  const [link, setLink] = useState<BankLink | null>(null);
  const [lastSync, setLastSync] = useState<BankSyncResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isBusy, setIsBusy] = useState(false);

  const run = useCallback(async (action: () => Promise<void>) => {
    setIsBusy(true);
    setError(null);
    try {
      await action();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setIsBusy(false);
    }
  }, []);

  useEffect(() => {
    setLink(null);
    setLastSync(null);
    api.bankLink.get().then(setLink).catch((err: Error) => setError(err.message));
  }, [activeWorkspaceId]);

  const loadAspsps = useCallback(() => api.bankLink.getAspsps('ES'), []);

  const startLink = useCallback(async (request: BankAuthorizationStart) => {
    const { url } = await api.bankLink.startAuthorization(request);
    if (activeWorkspaceId) sessionStorage.setItem(BANK_LINK_WORKSPACE_STORAGE_KEY, activeWorkspaceId);
    window.location.assign(url);
  }, [activeWorkspaceId]);

  const selectAccount = (accountUid: string) => run(async () => setLink(await api.bankLink.selectAccount(accountUid)));

  const sync = () => run(async () => {
    const result = await syncBankLink();
    setLastSync(result);
    setLink(result.link);
  });

  const unlink = () => run(async () => {
    await api.bankLink.unlink();
    setLastSync(null);
    setLink(await api.bankLink.get());
  });

  return { link, lastSync, error, isBusy, loadAspsps, startLink, selectAccount, sync, unlink };
}
