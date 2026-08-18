import { useMemo } from 'react';
import type { Account, JournalEntry } from '../types';

export interface AccountTotals {
  debit: number;
  credit: number;
}

export function isValidAccountCode(code: string) {
  return /^\d{4}$/.test(code);
}

export function getCashDisplayAccounts(accounts: Account[]) {
  return accounts
    .filter((account) => account.type === 'main' || account.type === 'space')
    .sort((a, b) => {
      if (a.type === 'main') return -1;
      if (b.type === 'main') return 1;
      return a.code.localeCompare(b.code);
    });
}

export function getSpaceAccounts(accounts: Account[]) {
  return getCashDisplayAccounts(accounts);
}

export function getEntityAccounts(accounts: Account[]) {
  return accounts.filter((account) => account.type === 'entity');
}

export function filterAndSortAccounts(accounts: Account[], searchTerm: string) {
  const search = searchTerm.toLowerCase();
  return [...accounts]
    .filter((account) => account.name.toLowerCase().includes(search) || account.code.includes(search))
    .sort((a, b) => a.code.localeCompare(b.code));
}

export function buildAccountStats(accounts: Account[], journalEntries: JournalEntry[]) {
  const stats: Record<string, AccountTotals> = {};
  accounts.forEach((account) => {
    stats[account.id] = { debit: 0, credit: 0 };
  });

  journalEntries.forEach((entry) => {
    entry.lines.forEach((line) => {
      if (stats[line.accountId]) {
        stats[line.accountId].debit += line.debit;
        stats[line.accountId].credit += line.credit;
      }
    });
  });

  return stats;
}

export function useDashboardViewModel(accounts: Account[]) {
  return useMemo(() => ({ displayAccounts: getCashDisplayAccounts(accounts) }), [accounts]);
}

export function useChartOfAccountsViewModel(
  accounts: Account[],
  journalEntries: JournalEntry[],
  searchTerm: string,
) {
  return useMemo(
    () => ({
      accountStats: buildAccountStats(accounts, journalEntries),
      filteredAccounts: filterAndSortAccounts(accounts, searchTerm),
    }),
    [accounts, journalEntries, searchTerm],
  );
}

export function useSpaceAccounts(accounts: Account[]) {
  return useMemo(() => getSpaceAccounts(accounts), [accounts]);
}

export function useEntityAccounts(accounts: Account[]) {
  return useMemo(() => getEntityAccounts(accounts), [accounts]);
}
