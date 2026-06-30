import { useState, useMemo } from 'react';
import { useFinanceStore } from '../store/useFinanceStore';
import { 
  Users, 
  Plus, 
  Search, 
  Tag, 
  MoreHorizontal,
  History,
} from 'lucide-react';
import { Account, JournalEntry, JournalLine } from '../types';

export function Entities() {
  const { accounts, journalEntries, addAccount } = useFinanceStore();
  const entities = useMemo(() => accounts.filter((a: Account) => a.type === 'entity'), [accounts]);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedEntity, setSelectedEntity] = useState<Account | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const [newEntity, setNewEntity] = useState({ name: '', code: '' });

  const handleAddEntity = () => {
    if (!newEntity.name || !newEntity.code) return;
    if (!/^\d{4}$/.test(newEntity.code)) {
      alert('El código debe ser numérico de 4 dígitos');
      return;
    }
    addAccount({ ...newEntity, type: 'entity', active: true });
    setIsAdding(false);
    setNewEntity({ name: '', code: '' });
  };

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const filteredEntities = useMemo(() => {
    const search = (searchTerm || '').toLowerCase();
    return entities.filter((e: Account) => {
      const name = (e.name || '').toLowerCase();
      return name.includes(search) || e.code.includes(search);
    }).sort((a, b) => a.code.localeCompare(b.code));
  }, [entities, searchTerm]);

  const selectedEntityStats = useMemo(() => {
    if (!selectedEntity) return null;
    const lines: (JournalLine & { 
      entryId: string; 
      date: string; 
      description: string; 
      displayAmount: number; 
    })[] = [];
    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach((line: JournalLine) => {
        if (line.accountId === selectedEntity.id) {
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
    
    const totalIn = lines.filter(l => l.displayAmount > 0).reduce((sum, l) => sum + l.displayAmount, 0);
    const totalOut = lines.filter(l => l.displayAmount < 0).reduce((sum, l) => sum + Math.abs(l.displayAmount), 0);
    
    return { 
      count: lines.length, 
      totalIn, 
      totalOut, 
      net: totalIn - totalOut,
      transactions: lines.sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime())
    };
  }, [selectedEntity, journalEntries]);

  const getEntitySummary = (id: string) => {
    let count = 0;
    let net = 0;
    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach((line: JournalLine) => {
        if (line.accountId === id) {
          count++;
          net += (line.debit - line.credit);
        }
      });
    });
    return { count, net };
  };

  return (
    <div className="flex flex-col gap-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-1">
          <h2 className="text-lg font-medium tracking-tight text-text-primary flex items-center gap-2">
            <Users className="w-4 h-4 text-text-secondary" />
            Entidades // Clientes, Proveedores y Otros
          </h2>
          <p className="text-xs text-text-secondary">Gestión de contrapartes y flujo de caja por entidad</p>
        </div>
        <button 
          onClick={() => setIsAdding(true)}
          className="bg-primary-green text-background px-4 py-1.5 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all flex items-center gap-2"
        >
          <Plus className="w-3.5 h-3.5" /> Nueva Entidad
        </button>
      </div>

      {isAdding && (
        <div className="bg-surface border border-border p-6 rounded-sm animate-in fade-in slide-in-from-top-2 duration-300">
          <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-6">Crear Nueva Entidad</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Código (4 dígitos)</label>
              <input 
                type="text" 
                value={newEntity.code}
                onChange={e => setNewEntity(prev => ({ ...prev, code: e.target.value.replace(/\D/g, '').slice(0, 4) }))}
                placeholder="Ej: 4300"
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Nombre</label>
              <input 
                type="text" 
                value={newEntity.name}
                onChange={e => setNewEntity(prev => ({ ...prev, name: e.target.value }))}
                placeholder="Ej: Cliente A"
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
              onClick={handleAddEntity}
              className="bg-primary-green text-background px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all"
            >
              Crear Entidad
            </button>
          </div>
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Entity List */}
        <div className="lg:col-span-1 flex flex-col gap-4">
          {/* Search */}
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-secondary" />
            <input 
              type="text" 
              placeholder="Buscar entidad..." 
              value={searchTerm}
              onChange={e => setSearchTerm(e.target.value)}
              className="w-full bg-surface border border-border pl-10 pr-4 py-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
            />
          </div>

          <div className="flex flex-col gap-2 max-h-[600px] overflow-y-auto pr-2 custom-scrollbar">
            {filteredEntities.map((e: Account) => {
              const summary = getEntitySummary(e.id);
              const isSelected = selectedEntity?.id === e.id;

              return (
                <button 
                  key={e.id}
                  onClick={() => setSelectedEntity(e)}
                  className={`bg-surface border p-4 text-left transition-all duration-200 group rounded-sm ${
                    isSelected ? 'border-primary-green ring-1 ring-primary-green/20' : 'border-border hover:border-border/60'
                  }`}
                >
                  <div className="flex items-center justify-between mb-3">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-sm bg-surface-elevated/50 border border-border flex items-center justify-center">
                        <Tag className="w-4 h-4 text-text-secondary" />
                      </div>
                      <div className="flex flex-col">
                        <span className="text-sm font-bold tracking-tight text-text-primary">{e.name}</span>
                        <span className="text-[9px] font-mono uppercase tracking-widest text-text-secondary">Código: {e.code}</span>
                      </div>
                    </div>
                    <MoreHorizontal className="w-4 h-4 text-text-secondary opacity-0 group-hover:opacity-100 transition-opacity" />
                  </div>
                  
                  <div className="flex items-end justify-between">
                    <div className="flex flex-col">
                      <span className="text-[9px] font-mono uppercase tracking-wider text-text-secondary mb-0.5">Flujo Neto</span>
                      <span className={`text-sm font-bold numeric ${summary.net >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                        {formatCurrency(summary.net)}
                      </span>
                    </div>
                    <span className="text-[9px] font-mono uppercase text-text-secondary">{summary.count} txs</span>
                  </div>
                </button>
              );
            })}
          </div>
        </div>

        {/* Entity Details / Statement */}
        <div className="lg:col-span-2">
          {selectedEntity && selectedEntityStats ? (
            <div className="flex flex-col gap-6 animate-in fade-in slide-in-from-right-4 duration-300">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-8 h-8 rounded-sm bg-surface-elevated/50 border border-border flex items-center justify-center">
                    <Tag className="w-4 h-4 text-text-secondary" />
                  </div>
                  <h3 className="text-lg font-medium tracking-tight text-text-primary uppercase tracking-widest">{selectedEntity.name}</h3>
                </div>
                <div className="flex items-center gap-2">
                  <button className="p-2 text-text-secondary hover:text-text-primary hover:bg-surface-elevated rounded-sm transition-all">
                    <MoreHorizontal className="w-4 h-4" />
                  </button>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <div className="bg-surface border border-border p-4 rounded-sm">
                  <h4 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-2">Entradas Totales</h4>
                  <p className="text-lg font-bold numeric text-primary-green">{formatCurrency(selectedEntityStats.totalIn)}</p>
                </div>
                <div className="bg-surface border border-border p-4 rounded-sm">
                  <h4 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-2">Salidas Totales</h4>
                  <p className="text-lg font-bold numeric text-primary-orange">{formatCurrency(selectedEntityStats.totalOut)}</p>
                </div>
                <div className="bg-surface border border-border p-4 rounded-sm">
                  <h4 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-2">Flujo Neto</h4>
                  <p className={`text-lg font-bold numeric ${selectedEntityStats.net >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                    {formatCurrency(selectedEntityStats.net)}
                  </p>
                </div>
              </div>

              <div className="bg-surface border border-border rounded-sm overflow-hidden">
                <div className="p-4 border-b border-border flex items-center justify-between bg-surface-elevated/20">
                  <h4 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary flex items-center gap-2">
                    <History className="w-3.5 h-3.5" /> Extracto Virtual de Movimientos
                  </h4>
                  <span className="text-[9px] font-mono text-text-secondary uppercase">{selectedEntityStats.count} movimientos</span>
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
                    {selectedEntityStats.transactions.map((tx) => (
                      <tr key={`${tx.entryId}-${tx.id}`} className="hover:bg-surface-elevated/10 transition-colors">
                        <td className="p-3 text-xs font-mono text-text-secondary">{tx.date.split('T')[0]}</td>
                        <td className="p-3 text-xs font-medium">{tx.description || 'Sin descripción'}</td>
                        <td className={`p-3 numeric font-bold text-right ${tx.displayAmount >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                          {formatCurrency(tx.displayAmount)}
                        </td>
                      </tr>
                    ))}
                    {selectedEntityStats.count === 0 && (
                      <tr key="empty-entities">
                        <td colSpan={3} className="p-8 text-center text-text-secondary italic text-xs">
                          No hay movimientos registrados para esta entidad.
                        </td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          ) : (
            <div className="h-full flex flex-col items-center justify-center text-center p-12 border border-dashed border-border rounded-sm bg-surface/30">
              <Users className="w-12 h-12 text-text-secondary opacity-20 mb-4" />
              <h3 className="text-sm font-medium text-text-secondary uppercase tracking-widest mb-2">Selecciona una entidad</h3>
              <p className="text-xs text-text-secondary max-w-[280px] leading-relaxed">
                Elige una entidad de la lista para ver su historial de transacciones y métricas de flujo.
              </p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
