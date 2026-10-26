import { BankLinkPanel } from '../components/BankLinkPanel';
import { BankStatement } from '../components/BankStatement';
import { useBankLink } from '../hooks/useBankLink';
import { useFinanceStore } from '../store/useFinanceStore';

const formatCurrency = (val: number) =>
  new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

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
  const bankLink = useBankLink();

  return (
    <div className="flex flex-col gap-6">
      <BankLinkPanel
        link={bankLink.link}
        lastSync={bankLink.lastSync}
        error={bankLink.error}
        isBusy={bankLink.isBusy}
        onLoadAspsps={bankLink.loadAspsps}
        onStartLink={bankLink.startLink}
        onSelectAccount={bankLink.selectAccount}
        onSync={bankLink.sync}
        onUnlink={bankLink.unlink}
        formatCurrency={formatCurrency}
      />
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
    </div>
  );
}
