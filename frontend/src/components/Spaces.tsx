import { useState, useMemo } from 'react';
import { useFinanceStore } from '../store/useFinanceStore';
import { 
  Plus, 
  PiggyBank, 
  History, 
  MoreHorizontal,
  Edit2,
  Trash2,
  ShieldCheck
} from 'lucide-react';
import { Account, JournalEntry, JournalLine } from '../types';

interface SpacesProps {
  bucketBalances: Record<string, number>;
}

export function Spaces({ bucketBalances }: SpacesProps) {
  const { accounts, journalEntries, addAccount, updateAccount, deleteAccount } = useFinanceStore();
  const buckets = useMemo(() => accounts.filter((a: Account) => a.type === 'space' || a.type === 'main'), [accounts]);
  
  const [selectedBucket, setSelectedBucket] = useState<Account | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const [newBucket, setNewBucket] = useState({ name: '', code: '' });

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const bucketTransactions = useMemo(() => {
    if (!selectedBucket) return [];
    const lines: (JournalLine & { 
      entryId: string; 
      date: string; 
      description: string; 
      displayAmount: number; 
    })[] = [];
    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach((line: JournalLine) => {
        if (line.accountId === selectedBucket.id) {
          lines.push({
            ...line,
            entryId: entry.id,
            date: entry.date,
            description: entry.description,
            displayAmount: line.debit - line.credit
          });
        }
      });
    });
    return lines.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  }, [selectedBucket, journalEntries]);

  const sortedBuckets = useMemo(() => {
    return [...buckets].sort((a, b) => {
      if (a.type === 'main') return -1;
      if (b.type === 'main') return 1;
      return a.code.localeCompare(b.code);
    });
  }, [buckets]);

  const handleAddBucket = () => {
    if (!newBucket.name || !newBucket.code) return;
    if (!/^\d{4}$/.test(newBucket.code)) {
      alert('El código debe ser numérico de 4 dígitos');
      return;
    }
    addAccount({ ...newBucket, type: 'space', active: true });
    setIsAdding(false);
    setNewBucket({ name: '', code: '' });
  };

  return (
    <div className="flex flex-col gap-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-1">
          <h2 className="text-lg font-medium tracking-tight text-text-primary flex items-center gap-2">
            <PiggyBank className="w-4 h-4 text-primary-orange" />
            Espacios de Reserva
          </h2>
          <p className="text-xs text-text-secondary">Gestión de fondos comprometidos y provisiones</p>
        </div>
        <button 
          onClick={() => setIsAdding(true)}
          className="bg-primary-green text-background px-4 py-1.5 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all flex items-center gap-2"
        >
          <Plus className="w-3.5 h-3.5" /> Nuevo Espacio
        </button>
      </div>

      {isAdding && (
        <div className="bg-surface border border-border p-6 rounded-sm animate-in fade-in slide-in-from-top-2 duration-300">
          <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-6">Crear Nuevo Espacio</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Código (4 dígitos)</label>
              <input 
                type="text" 
                value={newBucket.code}
                onChange={e => setNewBucket(prev => ({ ...prev, code: e.target.value.replace(/\D/g, '').slice(0, 4) }))}
                placeholder="Ej: 5722"
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Nombre</label>
              <input 
                type="text" 
                value={newBucket.name}
                onChange={e => setNewBucket(prev => ({ ...prev, name: e.target.value }))}
                placeholder="Ej: Reserva IVA"
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
              />
            </div>
          </div>
          <div className="flex justify-end gap-3">
            <button 
              onClick={() => setIsAdding(false)}
              className="px-6 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary hover:text-text-primary transition-all"
            >
              Cancelar
            </button>
            <button 
              onClick={handleAddBucket}
              className="bg-primary-green text-background px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all"
            >
              Crear Espacio
            </button>
          </div>
        </div>
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
                      <button className="p-2 text-text-secondary hover:text-text-primary hover:bg-surface-elevated rounded-sm transition-all">
                        <Edit2 className="w-4 h-4" />
                      </button>
                      <button onClick={() => deleteAccount(selectedBucket.id)} className="p-2 text-text-secondary hover:text-primary-orange hover:bg-primary-orange/10 rounded-sm transition-all">
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </>
                  )}
                </div>
              </div>

              <div className="bg-surface border border-border rounded-sm overflow-hidden">
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
              </div>
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
