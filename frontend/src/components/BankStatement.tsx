import { useState, useMemo } from 'react';
import { ImportCSV } from './ImportCSV';
import type { BankMovementImportCommitResult, BankMovementImportPreview, BankMovementImportRow, DuplicatePolicy } from '../api';
import { IdentifyModal } from './IdentifyModal';
import { ManualMovementForm, type ManualMovementDraft } from './ManualMovementForm';
import { MovementsTable } from './MovementsTable';
import { ReserveModal } from './ReserveModal';
import { PayFromSpaceModal } from './PayFromSpaceModal';
import { EditJournalEntryModal } from './EditJournalEntryModal';
import { 
  Plus, 
  Upload, 
} from 'lucide-react';
import { Account, BankMovement, JournalEntry, JournalLine } from '../types';
import {
  createEntryFromMovement,
  toEditableJournalEntry,
  validateJournalEntry,
} from '../hooks/journalEntryLogic';
import { Button } from '../design-system';

interface BankStatementProps {
  accounts: Account[];
  bankMovements: BankMovement[];
  journalEntries: JournalEntry[];
  onAddBankMovement: (movement: Omit<BankMovement, 'id' | 'isIdentified'>) => Promise<BankMovement>;
  onPreviewBankMovementImport: (file: File) => Promise<BankMovementImportPreview>;
  onCommitBankMovementImport: (rows: BankMovementImportRow[], duplicatePolicy?: DuplicatePolicy) => Promise<BankMovementImportCommitResult>;
  onUpdateBankMovement: (id: string, updates: Partial<BankMovement>) => Promise<void>;
  onDeleteBankMovement: (id: string) => Promise<void>;
  onAddJournalEntry: (entry: Omit<JournalEntry, 'id'>) => Promise<JournalEntry>;
  onUpdateJournalEntry: (id: string, updates: Partial<JournalEntry>) => Promise<void>;
}

export function BankStatement({
  accounts,
  bankMovements,
  journalEntries,
  onAddBankMovement,
  onPreviewBankMovementImport,
  onCommitBankMovementImport,
  onUpdateBankMovement,
  onDeleteBankMovement,
  onAddJournalEntry,
  onUpdateJournalEntry,
}: BankStatementProps) {

  const [isAdding, setIsAdding] = useState(false);
  const [newMovement, setNewMovement] = useState<ManualMovementDraft>({
    date: new Date().toISOString().split('T')[0],
    description: '',
    amount: ''
  });
  const [isImporting, setIsImporting] = useState(false);
  const [editingMovement, setEditingMovement] = useState<BankMovement | null>(null);
  const [identifyingMovement, setIdentifyingMovement] = useState<BankMovement | null>(null);
  const [reservingMovement, setReservingMovement] = useState<BankMovement | null>(null);
  const [payingFromSpaceMovement, setPayingFromSpaceMovement] = useState<BankMovement | null>(null);

  // Journal Entry Editing State
  const [editingEntry, setEditingEntry] = useState<{
    description: string;
    date: string;
    lines: JournalLine[];
  } | null>(null);

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const bankBalance = useMemo(() => {
    return bankMovements.reduce((sum: number, m: BankMovement) => sum + m.amount, 0);
  }, [bankMovements]);

  const mainAccount = useMemo(() => accounts.find((a: Account) => a.type === 'main'), [accounts]);
  const uncategorizedAccount = useMemo(() => accounts.find((a: Account) => a.type === 'uncategorized'), [accounts]);
  
  const sortedBankMovements = useMemo(() => {
    return [...bankMovements].sort((a, b) => {
      const dateA = new Date(a.date).getTime();
      const dateB = new Date(b.date).getTime();
      return dateB - dateA;
    });
  }, [bankMovements]);

  const handleAddMovement = () => {
    if (!newMovement.description || !newMovement.amount) return;
    onAddBankMovement({
      date: newMovement.date,
      description: newMovement.description,
      amount: parseFloat(newMovement.amount)
    });
    setIsAdding(false);
    setNewMovement({
      date: new Date().toISOString().split('T')[0],
      description: '',
      amount: ''
    });
  };

  const getOrCreateEntry = async (movement: BankMovement) => {
    if (movement.journalEntryId) {
      return journalEntries.find((e: JournalEntry) => e.id === movement.journalEntryId);
    }
    
    if (!mainAccount || !uncategorizedAccount) {
      console.error('Missing system accounts:', { mainAccount, uncategorizedAccount });
      alert('Error: No se han configurado las cuentas de sistema (Principal/Sin categorizar)');
      return undefined;
    }

    const entry = await onAddJournalEntry(createEntryFromMovement(movement, mainAccount.id, uncategorizedAccount.id));

    await onUpdateBankMovement(movement.id, { journalEntryId: entry.id });
    return entry;
  };

  const startEditingEntry = async (movement: BankMovement) => {
    setEditingMovement(movement);
    
    const entry = await getOrCreateEntry(movement);
    if (!entry) return;

    setEditingEntry(toEditableJournalEntry(entry));
  };

  const handleSaveEntry = async (updatedEntry: {
    description: string;
    date: string;
    lines: JournalLine[];
  }) => {
    if (!editingMovement) return;

    const validationError = validateJournalEntry(updatedEntry);
    if (validationError) {
      alert(validationError);
      return;
    }

    await onUpdateJournalEntry(editingMovement.journalEntryId!, {
      date: updatedEntry.date,
      description: updatedEntry.description,
      lines: updatedEntry.lines.map(l => new JournalLine(l))
    });

    setEditingMovement(null);
    setEditingEntry(null);
  };

  const handleImportCSV = () => {
    setIsImporting(true);
  };

  const handleUpdateDescription = async (id: string, description: string) => {
    const movement = bankMovements.find(m => m.id === id);
    if (!movement) return;

    await onUpdateBankMovement(id, { description });

    if (movement.journalEntryId) {
      await onUpdateJournalEntry(movement.journalEntryId, { description });
    }
  };

  return (
    <div className="flex flex-col gap-8">
      {/* Header & Balance */}
      <div className="flex flex-col md:flex-row md:items-end justify-between gap-6">
        <div className="flex flex-col gap-2">
          <h2 className="text-[10px] font-mono uppercase tracking-[0.2em] text-text-secondary">Posición Global Bancaria</h2>
          <div className="flex items-baseline gap-3">
            <span className="text-4xl font-bold tracking-tighter numeric">
              {formatCurrency(bankBalance)}
            </span>
            <span className="text-xs font-mono text-text-secondary uppercase tracking-widest">Saldo Real</span>
          </div>
        </div>
        
        <div className="flex items-center gap-3">
          <Button
            variant="secondary"
            onClick={handleImportCSV}
            icon={<Upload className="w-3.5 h-3.5" />}
          >
            Importar
          </Button>
          <Button
            size="sm"
            onClick={() => setIsAdding(true)}
            icon={<Plus className="w-3.5 h-3.5" />}
          >
            Nuevo Movimiento
          </Button>
        </div>
      </div>

      {isAdding && (
        <ManualMovementForm
          movement={newMovement}
          onChange={setNewMovement}
          onCancel={() => setIsAdding(false)}
          onSubmit={handleAddMovement}
        />
      )}

      <MovementsTable
        accounts={accounts}
        bankMovements={sortedBankMovements}
        onDeleteBankMovement={onDeleteBankMovement}
        onEditEntry={startEditingEntry}
        onIdentifyMovement={setIdentifyingMovement}
        onPayFromSpace={setPayingFromSpaceMovement}
        onReserveMovement={setReservingMovement}
        onUpdateDescription={handleUpdateDescription}
        formatCurrency={formatCurrency}
      />

      {/* Import Modal */}
      {isImporting && (
        <ImportCSV
          onClose={() => setIsImporting(false)}
          onPreview={onPreviewBankMovementImport}
          onCommit={onCommitBankMovementImport}
        />
      )}

      {/* Identify Modal */}
      {identifyingMovement && (
        <IdentifyModal 
          movement={identifyingMovement} 
          accounts={accounts}
          onClose={() => setIdentifyingMovement(null)} 
          getOrCreateEntry={getOrCreateEntry} 
          onUpdateJournalEntry={onUpdateJournalEntry}
          onUpdateBankMovement={onUpdateBankMovement}
        />
      )}

      {/* Reserve Modal */}
      {reservingMovement && (
        <ReserveModal 
          movement={reservingMovement} 
          accounts={accounts}
          onClose={() => setReservingMovement(null)} 
          getOrCreateEntry={getOrCreateEntry} 
          onUpdateJournalEntry={onUpdateJournalEntry}
          formatCurrency={formatCurrency}
        />
      )}

      {/* Pay from Space Modal */}
      {payingFromSpaceMovement && (
        <PayFromSpaceModal 
          movement={payingFromSpaceMovement} 
          accounts={accounts}
          onClose={() => setPayingFromSpaceMovement(null)} 
          getOrCreateEntry={getOrCreateEntry} 
          onUpdateJournalEntry={onUpdateJournalEntry}
        />
      )}

      {/* Edit Journal Entry Modal */}
      {editingMovement && editingEntry && (
        <EditJournalEntryModal 
          movement={editingMovement}
          entry={editingEntry}
          accounts={accounts}
          onClose={() => setEditingMovement(null)}
          onSave={handleSaveEntry}
          setEntry={setEditingEntry}
          formatCurrency={formatCurrency}
        />
      )}
    </div>
  );
}
