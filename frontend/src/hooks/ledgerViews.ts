import { useMemo } from 'react';
import type { Account, JournalEntry, JournalLine } from '../types';

export type LedgerLine = JournalLine & {
  entryId: string;
  date: string;
  description: string;
};

export type AccountLedgerLine = LedgerLine & {
  displayAmount: number;
};

export interface AccountLedgerStats {
  count: number;
  totalIn: number;
  totalOut: number;
  net: number;
  transactions: AccountLedgerLine[];
}

export function sortLedgerLinesNewestFirst<T extends { date: string }>(lines: T[]) {
  return [...lines].sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
}

export function flattenJournalLines(journalEntries: JournalEntry[]) {
  const lines: LedgerLine[] = [];
  journalEntries.forEach((entry) => {
    entry.lines.forEach((line) => {
      lines.push({
        ...line,
        entryId: entry.id,
        date: entry.date,
        description: entry.description,
      });
    });
  });
  return lines;
}

export function getAccountTransactions(journalEntries: JournalEntry[], accountId?: string) {
  if (!accountId) return [];

  const lines: AccountLedgerLine[] = [];
  journalEntries.forEach((entry) => {
    entry.lines.forEach((line) => {
      if (line.accountId === accountId) {
        lines.push({
          ...line,
          entryId: entry.id,
          date: entry.date,
          description: entry.description,
          displayAmount: line.debit - line.credit,
        });
      }
    });
  });

  return sortLedgerLinesNewestFirst(lines);
}

export function getAccountLedgerStats(journalEntries: JournalEntry[], accountId?: string): AccountLedgerStats | null {
  if (!accountId) return null;

  const transactions = getAccountTransactions(journalEntries, accountId);
  const totalIn = transactions
    .filter((line) => line.displayAmount > 0)
    .reduce((sum, line) => sum + line.displayAmount, 0);
  const totalOut = transactions
    .filter((line) => line.displayAmount < 0)
    .reduce((sum, line) => sum + Math.abs(line.displayAmount), 0);

  return {
    count: transactions.length,
    totalIn,
    totalOut,
    net: totalIn - totalOut,
    transactions,
  };
}

export function getAccountSummary(journalEntries: JournalEntry[], accountId: string) {
  return getAccountTransactions(journalEntries, accountId).reduce(
    (summary, line) => ({
      count: summary.count + 1,
      net: summary.net + line.displayAmount,
    }),
    { count: 0, net: 0 },
  );
}

export function filterJournalLines(lines: LedgerLine[], accounts: Account[], searchTerm: string) {
  const search = searchTerm.toLowerCase();
  return sortLedgerLinesNewestFirst(
    lines.filter((line) => {
      const account = accounts.find((item) => item.id === line.accountId);
      return (
        line.description.toLowerCase().includes(search) ||
        account?.name.toLowerCase().includes(search) ||
        account?.code.includes(search)
      );
    }),
  );
}

export function useSpaceLedger(journalEntries: JournalEntry[], selectedAccount: Account | null) {
  return useMemo(
    () => getAccountTransactions(journalEntries, selectedAccount?.id),
    [journalEntries, selectedAccount],
  );
}

export function useEntityLedger(journalEntries: JournalEntry[], selectedAccount: Account | null) {
  return useMemo(
    () => getAccountLedgerStats(journalEntries, selectedAccount?.id),
    [journalEntries, selectedAccount],
  );
}

export function useJournalLines(journalEntries: JournalEntry[], accounts: Account[], searchTerm: string) {
  return useMemo(
    () => filterJournalLines(flattenJournalLines(journalEntries), accounts, searchTerm),
    [journalEntries, accounts, searchTerm],
  );
}
