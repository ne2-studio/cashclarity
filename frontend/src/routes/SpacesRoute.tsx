import { useMemo } from 'react';
import { Spaces } from '../components/Spaces';
import { useFinanceStore } from '../store/useFinanceStore';
import { JournalEntry } from '../types';

export function SpacesRoute() {
  const { accounts, journalEntries, addAccount, deleteAccount } = useFinanceStore();

  const bucketBalances = useMemo(() => {
    const accountBalances: Record<string, number> = {};
    accounts.forEach(a => accountBalances[a.id] = 0);

    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach(line => {
        if (accountBalances[line.accountId] !== undefined) {
          accountBalances[line.accountId] += line.debit - line.credit;
        }
      });
    });

    return accountBalances;
  }, [accounts, journalEntries]);

  return (
    <Spaces
      accounts={accounts}
      journalEntries={journalEntries}
      bucketBalances={bucketBalances}
      onAddAccount={addAccount}
      onDeleteAccount={deleteAccount}
    />
  );
}
