import { Entities } from '../components/Entities';
import { useFinanceStore } from '../store/useFinanceStore';

export function EntitiesRoute() {
  const { accounts, journalEntries, addAccount } = useFinanceStore();

  return (
    <Entities
      accounts={accounts}
      journalEntries={journalEntries}
      onAddAccount={addAccount}
    />
  );
}
