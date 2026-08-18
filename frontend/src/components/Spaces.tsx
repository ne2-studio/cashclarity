import { useState } from 'react';
import { 
  Plus, 
  PiggyBank, 
  History, 
  MoreHorizontal,
  Edit2,
  Trash2,
  ShieldCheck
} from 'lucide-react';
import { Account, JournalEntry } from '../types';
import { isValidAccountCode, useSpaceAccounts } from '../hooks/accountViews';
import { useSpaceLedger } from '../hooks/ledgerViews';
import { Button, Card, IconButton, PageHeader } from '../design-system';
import { CreateSpaceForm, type SpaceDraft } from './CreateSpaceForm';

interface SpacesProps {
  accounts: Account[];
  journalEntries: JournalEntry[];
  bucketBalances: Record<string, number>;
  onAddAccount: (account: Omit<Account, 'id'>) => Promise<Account>;
  onDeleteAccount: (id: string) => Promise<void>;
}

export function Spaces({ accounts, journalEntries, bucketBalances, onAddAccount, onDeleteAccount }: SpacesProps) {
  const [selectedBucket, setSelectedBucket] = useState<Account | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const [newBucket, setNewBucket] = useState<SpaceDraft>({ name: '', code: '' });
  const sortedBuckets = useSpaceAccounts(accounts);
  const bucketTransactions = useSpaceLedger(journalEntries, selectedBucket);

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const handleAddBucket = () => {
    if (!newBucket.name || !newBucket.code) return;
    if (!isValidAccountCode(newBucket.code)) {
      alert('El código debe ser numérico de 4 dígitos');
      return;
    }
    onAddAccount({ ...newBucket, type: 'space', active: true });
    setIsAdding(false);
    setNewBucket({ name: '', code: '' });
  };

  return (
    <div className="flex flex-col gap-8">
      {/* Header */}
      <PageHeader
        icon={<PiggyBank className="w-4 h-4 text-primary-orange" />}
        title="Espacios de Reserva"
        subtitle="Gestión de fondos comprometidos y provisiones"
        actions={(
          <Button size="sm" onClick={() => setIsAdding(true)} icon={<Plus className="w-3.5 h-3.5" />}>
            Nuevo Espacio
          </Button>
        )}
      />

      {isAdding && (
        <CreateSpaceForm
          space={newBucket}
          onCancel={() => setIsAdding(false)}
          onChange={setNewBucket}
          onSubmit={handleAddBucket}
        />
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Bucket List */}
        <div className="lg:col-span-1 flex flex-col gap-4">
          {sortedBuckets.map((b: Account) => {
            const balance = bucketBalances[b.id] || 0;
            const isSelected = selectedBucket?.id === b.id;
            const isMain = b.type === 'main';

            return (
              <button 
                key={b.id}
                onClick={() => setSelectedBucket(b)}
                className={`bg-surface border p-5 text-left transition-all duration-200 group rounded-sm ${
                  isSelected 
                    ? (isMain ? 'border-primary-green ring-1 ring-primary-green/20' : 'border-primary-orange ring-1 ring-primary-orange/20') 
                    : 'border-border hover:border-border/60'
                } ${isMain ? 'bg-primary-green/5' : ''}`}
              >
                <div className="flex items-center justify-between mb-4">
                  <div className="flex items-center gap-3">
                    <div className={`w-8 h-8 rounded-sm border flex items-center justify-center ${isMain ? 'bg-primary-green/20 border-primary-green/30' : 'bg-surface-elevated/50 border-border'}`}>
                      {isMain ? <ShieldCheck className="w-4 h-4 text-primary-green" /> : <PiggyBank className="w-4 h-4 text-text-secondary" />}
                    </div>
                    <div className="flex flex-col">
                      <h3 className={`text-sm font-bold tracking-tight ${isMain ? 'text-primary-green' : ''}`}>
                        {isMain ? 'Cuenta Principal' : b.name}
                      </h3>
                      {isMain && (
                        <span className="text-[8px] font-mono uppercase tracking-widest text-primary-green/70">
                          Fondos Disponibles
                        </span>
                      )}
                    </div>
                  </div>
                  {!isMain && <MoreHorizontal className="w-4 h-4 text-text-secondary opacity-0 group-hover:opacity-100 transition-opacity" />}
                </div>
                
                <div className="flex items-end justify-between">
                  <div className="flex flex-col">
                    <span className="text-[10px] font-mono uppercase tracking-wider text-text-secondary mb-1">Saldo Actual</span>
                    <span className={`text-lg font-bold numeric ${balance >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                      {formatCurrency(balance)}
                    </span>
                  </div>
                </div>
              </button>
            );
          })}
        </div>

        {/* Bucket Details / Statement */}
        <div className="lg:col-span-2">
          {selectedBucket ? (
            <div className="flex flex-col gap-6 animate-in fade-in slide-in-from-right-4 duration-300">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-sm border flex items-center justify-center ${selectedBucket.type === 'main' ? 'bg-primary-green/20 border-primary-green/30' : 'bg-surface-elevated/50 border-border'}`}>
                    {selectedBucket.type === 'main' ? <ShieldCheck className="w-4 h-4 text-primary-green" /> : <PiggyBank className="w-4 h-4 text-text-secondary" />}
                  </div>
                  <h3 className={`text-lg font-medium tracking-tight uppercase tracking-widest flex items-center gap-2 ${selectedBucket.type === 'main' ? 'text-primary-green' : 'text-text-primary'}`}>
                    {selectedBucket.type === 'main' ? 'Cuenta Principal' : selectedBucket.name}
                    <span className="text-[10px] font-mono text-text-secondary opacity-60">
                      ({selectedBucket.code})
                    </span>
                  </h3>
                </div>
                <div className="flex items-center gap-2">
                  {selectedBucket.type !== 'main' && (
                    <>
                      <IconButton size="md" hover="default" aria-label="Editar espacio">
                        <Edit2 className="w-4 h-4" />
                      </IconButton>
                      <IconButton size="md" onClick={() => onDeleteAccount(selectedBucket.id)} aria-label="Eliminar espacio">
                        <Trash2 className="w-4 h-4" />
                      </IconButton>
                    </>
                  )}
                </div>
              </div>

              <Card className="overflow-x-auto">
                <div className="p-4 border-b border-border flex items-center justify-between bg-surface-elevated/20">
                  <h4 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary flex items-center gap-2">
                    <History className="w-3.5 h-3.5" /> Extracto Virtual de Movimientos
                  </h4>
                  <span className="text-[9px] font-mono text-text-secondary uppercase">{bucketTransactions.length} movimientos</span>
                </div>
                <table className="w-full text-left">
                  <thead>
                    <tr className="bg-surface-elevated/10 border-b border-border">
                      <th className="p-3 text-[9px] font-mono uppercase tracking-widest text-text-secondary">Fecha</th>
                      <th className="p-3 text-[9px] font-mono uppercase tracking-widest text-text-secondary">Descripción</th>
                      <th className="p-3 text-[9px] font-mono uppercase tracking-widest text-text-secondary text-right">Importe</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/30">
                    {bucketTransactions.map((tx) => (
                      <tr key={`${tx.entryId}-${tx.id}`} className="hover:bg-surface-elevated/10 transition-colors">
                        <td className="p-3 text-xs font-mono text-text-secondary">{tx.date.split('T')[0]}</td>
                        <td className="p-3 text-xs font-medium">{tx.description || 'Sin descripción'}</td>
                        <td className={`p-3 numeric font-bold text-right ${tx.displayAmount >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                          {formatCurrency(tx.displayAmount)}
                        </td>
                      </tr>
                    ))}
                    {bucketTransactions.length === 0 && (
                      <tr key="empty-spaces">
                        <td colSpan={3} className="p-8 text-center text-text-secondary italic text-xs">
                          No hay movimientos asignados a este espacio aún.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </Card>
            </div>
          ) : (
            <div className="h-full flex flex-col items-center justify-center text-center p-12 border border-dashed border-border rounded-sm bg-surface/30">
              <PiggyBank className="w-12 h-12 text-text-secondary opacity-20 mb-4" />
              <h3 className="text-sm font-medium text-text-secondary uppercase tracking-widest mb-2">Selecciona un espacio</h3>
              <p className="text-xs text-text-secondary max-w-[280px] leading-relaxed">
                Elige un espacio de la lista para ver su extracto virtual y detalles de asignación.
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
