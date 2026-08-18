import { useMemo } from 'react';
import { Dashboard } from '../components/Dashboard';
import { useFinanceStore } from '../store/useFinanceStore';
import { JournalEntry } from '../types';

export function DashboardRoute() {
  const { accounts, journalEntries } = useFinanceStore();

  const treasuryMetrics = useMemo(() => {
    const accountBalances: Record<string, number> = {};
    accounts.forEach(a => accountBalances[a.id] = 0);

    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach(line => {
        if (accountBalances[line.accountId] !== undefined) {
          accountBalances[line.accountId] += (line.debit - line.credit);
        }
      });
    });

    const mainAccount = accounts.find(a => a.type === 'main');
    const spaceAccounts = accounts.filter(a => a.type === 'space');
    const totalCommitted = spaceAccounts.reduce((sum, a) => sum + accountBalances[a.id], 0);
    const mainBalance = mainAccount ? accountBalances[mainAccount.id] : 0;

    return {
      realBankBalance: mainBalance + totalCommitted,
      totalCommitted,
      availableCash: mainBalance,
      bucketBalances: accountBalances,
    };
  }, [accounts, journalEntries]);

  return <Dashboard accounts={accounts} metrics={treasuryMetrics} />;
}
