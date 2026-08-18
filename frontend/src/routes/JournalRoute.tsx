import { Journal } from '../components/Journal';
import { useFinanceStore } from '../store/useFinanceStore';

export function JournalRoute() {
  const { journalEntries, accounts } = useFinanceStore();

  return <Journal journalEntries={journalEntries} accounts={accounts} />;
}
