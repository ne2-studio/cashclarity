import { useState, useMemo } from 'react';
import { 
  Users, 
  Plus, 
  Tag, 
  MoreHorizontal,
  History,
} from 'lucide-react';
import { Account, JournalEntry } from '../types';
import { getAccountSummary, useEntityLedger } from '../hooks/ledgerViews';
import { getEntityAccounts, isValidAccountCode } from '../hooks/accountViews';
import { Button, Card, IconButton, PageHeader, SearchField, StatCard } from '../design-system';
import { CreateEntityForm, type EntityDraft } from './CreateEntityForm';

interface EntitiesProps {
  accounts: Account[];
  journalEntries: JournalEntry[];
  onAddAccount: (account: Omit<Account, 'id'>) => Promise<Account>;
}

export function Entities({ accounts, journalEntries, onAddAccount }: EntitiesProps) {
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedEntity, setSelectedEntity] = useState<Account | null>(null);
  const [isAdding, setIsAdding] = useState(false);
  const [newEntity, setNewEntity] = useState<EntityDraft>({ name: '', code: '' });

  const selectedEntityStats = useEntityLedger(journalEntries, selectedEntity);

  const handleAddEntity = () => {
    if (!newEntity.name || !newEntity.code) return;
    if (!isValidAccountCode(newEntity.code)) {
      alert('El código debe ser numérico de 4 dígitos');
      return;
    }
    onAddAccount({ ...newEntity, type: 'entity', active: true });
    setIsAdding(false);
    setNewEntity({ name: '', code: '' });
  };

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const filteredEntities = useMemo(() => {
    const search = (searchTerm || '').toLowerCase();
    return getEntityAccounts(accounts).filter((e: Account) => {
      const name = (e.name || '').toLowerCase();
      return name.includes(search) || e.code.includes(search);
    }).sort((a, b) => a.code.localeCompare(b.code));
  }, [accounts, searchTerm]);

  const getEntitySummary = (id: string) => {
    return getAccountSummary(journalEntries, id);
  };

  return (
    <div className="flex flex-col gap-8">
      {/* Header */}
      <PageHeader
        icon={<Users className="w-4 h-4 text-text-secondary" />}
        title="Entidades // Clientes, Proveedores y Otros"
        subtitle="Gestión de contrapartes y flujo de caja por entidad"
        actions={(
          <Button size="sm" onClick={() => setIsAdding(true)} icon={<Plus className="w-3.5 h-3.5" />}>
            Nueva Entidad
          </Button>
        )}
      />

      {isAdding && (
        <CreateEntityForm
          entity={newEntity}
          onCancel={() => setIsAdding(false)}
          onChange={setNewEntity}
          onSubmit={handleAddEntity}
        />
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Entity List */}
        <div className="lg:col-span-1 flex flex-col gap-4">
          {/* Search */}
          <SearchField
            placeholder="Buscar entidad..."
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
          />

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
                  <IconButton size="md" hover="default" aria-label="Más opciones">
                    <MoreHorizontal className="w-4 h-4" />
                  </IconButton>
                </div>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                <StatCard title="Entradas Totales" value={formatCurrency(selectedEntityStats.totalIn)} tone="positive" />
                <StatCard title="Salidas Totales" value={formatCurrency(selectedEntityStats.totalOut)} tone="warning" />
                <StatCard title="Flujo Neto" value={formatCurrency(selectedEntityStats.net)} tone={selectedEntityStats.net >= 0 ? 'positive' : 'warning'} />
              </div>

              <Card className="overflow-x-auto">
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
              </Card>
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
