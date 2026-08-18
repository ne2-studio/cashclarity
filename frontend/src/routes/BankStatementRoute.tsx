import { BankStatement } from '../components/BankStatement';
import { useFinanceStore } from '../store/useFinanceStore';

export function BankStatementRoute() {
  const {
    accounts,
    bankMovements,
    journalEntries,
    addBankMovement,
    previewBankMovementImport,
    commitBankMovementImport,
    updateBankMovement,
    deleteBankMovement,
    addJournalEntry,
    updateJournalEntry,
  } = useFinanceStore();

  return (
    <BankStatement
      accounts={accounts}
      bankMovements={bankMovements}
      journalEntries={journalEntries}
      onAddBankMovement={addBankMovement}
      onPreviewBankMovementImport={previewBankMovementImport}
      onCommitBankMovementImport={commitBankMovementImport}
      onUpdateBankMovement={updateBankMovement}
      onDeleteBankMovement={deleteBankMovement}
      onAddJournalEntry={addJournalEntry}
      onUpdateJournalEntry={updateJournalEntry}
    />
  );
}
