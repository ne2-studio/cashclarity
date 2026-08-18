import { ChartOfAccounts } from '../components/ChartOfAccounts';
import { useFinanceStore } from '../store/useFinanceStore';

export function ChartOfAccountsRoute() {
  const { accounts, addAccount, deleteAccount, journalEntries } = useFinanceStore();

  return (
    <ChartOfAccounts
      accounts={accounts}
      journalEntries={journalEntries}
      onAddAccount={addAccount}
      onDeleteAccount={deleteAccount}
    />
  );
}
