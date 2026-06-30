import React, { useState, useMemo } from 'react';
import { useFinanceStore } from '../store/useFinanceStore';
import { ImportCSV } from './ImportCSV';
import { IdentifyModal } from './IdentifyModal';
import { ReserveModal } from './ReserveModal';
import { PayFromSpaceModal } from './PayFromSpaceModal';
import { EditJournalEntryModal } from './EditJournalEntryModal';
import { 
  Plus, 
  Upload, 
  CheckCircle2, 
  Circle, 
  X, 
  AlertCircle,
  ArrowUpRight,
  ArrowDownLeft,
  Search,
  Trash2,
  FileEdit,
  PiggyBank,
  Wallet
} from 'lucide-react';
import { Account, BankMovement, JournalEntry, JournalLine } from '../types';

export function BankStatement() {
  const { 
    accounts, 
    bankMovements, 
    addBankMovement, 
    updateBankMovement, 
    deleteBankMovement,
    addJournalEntry,
    updateJournalEntry,
    journalEntries
  } = useFinanceStore();

  const [isAdding, setIsAdding] = useState(false);
  const [newMovement, setNewMovement] = useState({
    date: new Date().toISOString().split('T')[0],
    description: '',
    amount: ''
  });
  const [isImporting, setIsImporting] = useState(false);
  const [editingMovement, setEditingMovement] = useState<BankMovement | null>(null);
  const [identifyingMovement, setIdentifyingMovement] = useState<BankMovement | null>(null);
  const [reservingMovement, setReservingMovement] = useState<BankMovement | null>(null);
  const [payingFromSpaceMovement, setPayingFromSpaceMovement] = useState<BankMovement | null>(null);
  const [editingDescriptionId, setEditingDescriptionId] = useState<string | null>(null);
  const [tempDescription, setTempDescription] = useState('');

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
    addBankMovement({
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

    const isIncome = movement.amount > 0;
    const absAmount = Math.abs(movement.amount);

    const entry = await addJournalEntry({
      description: movement.description,
      date: movement.date,
      lines: [
        new JournalLine({ 
          id: crypto.randomUUID(),
          accountId: mainAccount.id,
          debit: isIncome ? absAmount : 0, 
          credit: isIncome ? 0 : absAmount 
        }),
        new JournalLine({ 
          id: crypto.randomUUID(),
          accountId: uncategorizedAccount.id,
          debit: isIncome ? 0 : absAmount, 
          credit: isIncome ? absAmount : 0 
        })
      ]
    });

    await updateBankMovement(movement.id, { journalEntryId: entry.id });
    return entry;
  };

  const startEditingEntry = async (movement: BankMovement) => {
    setEditingMovement(movement);
    
    const entry = await getOrCreateEntry(movement);
    if (!entry) return;

    setEditingEntry({
      description: entry.description,
      date: entry.date,
      lines: entry.lines.map((l: JournalLine) => ({ ...l }))
    });
  };

  const handleSaveEntry = async (updatedEntry: {
    description: string;
    date: string;
    lines: JournalLine[];
  }) => {
    if (!editingMovement) return;

    if (updatedEntry.lines.some(l => !l.accountId)) {
      alert('Todas las líneas deben tener una cuenta seleccionada');
      return;
    }

    const totalDebit = updatedEntry.lines.reduce((sum, l) => sum + l.debit, 0);
    const totalCredit = updatedEntry.lines.reduce((sum, l) => sum + l.credit, 0);

    if (Math.abs(totalDebit - totalCredit) > 0.01) {
      alert('El asiento no está cuadrado');
      return;
    }

    await updateJournalEntry(editingMovement.journalEntryId!, {
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

  const handleUpdateDescription = async (id: string) => {
    if (!tempDescription.trim()) {
      setEditingDescriptionId(null);
      return;
    }
    
    const movement = bankMovements.find(m => m.id === id);
    if (!movement) return;

    await updateBankMovement(id, { description: tempDescription });

    if (movement.journalEntryId) {
      await updateJournalEntry(movement.journalEntryId, { description: tempDescription });
    }

    setEditingDescriptionId(null);
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
          <button 
            onClick={handleImportCSV}
            className="flex items-center gap-2 px-4 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary border border-border hover:bg-surface-elevated transition-all rounded-sm"
          >
            <Upload className="w-3.5 h-3.5" /> Importar CSV
          </button>
          <button 
            onClick={() => setIsAdding(true)}
            className="flex items-center gap-2 px-4 py-2 text-xs font-bold uppercase tracking-widest bg-primary-green text-background hover:bg-primary-green/90 transition-all rounded-sm"
          >
            <Plus className="w-3.5 h-3.5" /> Nuevo Movimiento
          </button>
        </div>
      </div>

      {isAdding && (
        <div className="bg-surface border border-border p-6 rounded-sm animate-in fade-in slide-in-from-top-2 duration-300">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary">Registrar Movimiento Manual</h3>
            <button onClick={() => setIsAdding(false)} className="text-text-secondary hover:text-text-primary">
              <X className="w-4 h-4" />
            </button>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Fecha</label>
              <input 
                type="date" 
                value={newMovement.date}
                onChange={e => setNewMovement(prev => ({ ...prev, date: e.target.value }))}
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-orange outline-none"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Concepto</label>
              <input 
                type="text" 
                value={newMovement.description}
                onChange={e => setNewMovement(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Ej: Transferencia Recibida"
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-orange outline-none"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Importe (+ Ingreso, - Gasto)</label>
              <input 
                type="number" 
                step="0.01"
                value={newMovement.amount}
                onChange={e => setNewMovement(prev => ({ ...prev, amount: e.target.value }))}
                placeholder="0.00"
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-orange outline-none font-mono"
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
              onClick={handleAddMovement}
              className="bg-primary-green text-background px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all"
            >
              Guardar Movimiento
            </button>
          </div>
        </div>
      )}

      {/* Movements Table */}
      <div className="bg-surface border border-border rounded-sm overflow-hidden">
        <div className="p-4 border-b border-border flex items-center justify-between bg-surface-elevated/20">
          <h4 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary">Extracto de Movimientos Bancarios</h4>
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-1.5">
              <div className="w-2 h-2 rounded-full bg-primary-green"></div>
              <span className="text-[9px] font-mono text-text-secondary uppercase">Identificado</span>
            </div>
            <div className="flex items-center gap-1.5">
              <div className="w-2 h-2 rounded-full bg-primary-orange"></div>
              <span className="text-[9px] font-mono text-text-secondary uppercase">Pendiente</span>
            </div>
          </div>
        </div>
        <table className="w-full text-left">
          <thead>
            <tr className="bg-surface-elevated/10 border-b border-border">
              <th className="p-4 text-[9px] font-mono uppercase tracking-widest text-text-secondary w-16 text-center">Estado</th>
              <th className="p-4 text-[9px] font-mono uppercase tracking-widest text-text-secondary">Fecha</th>
              <th className="p-4 text-[9px] font-mono uppercase tracking-widest text-text-secondary">Concepto</th>
              <th className="p-4 text-[9px] font-mono uppercase tracking-widest text-text-secondary">Entidad</th>
              <th className="p-4 text-[9px] font-mono uppercase tracking-widest text-text-secondary text-right">Cantidad</th>
              <th className="p-4 text-[9px] font-mono uppercase tracking-widest text-text-secondary text-right">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/30">
            {sortedBankMovements.map((m: BankMovement) => {
              const entity = accounts.find((a: Account) => a.id === m.entityId);
              
              return (
                <tr key={m.id} className="hover:bg-surface-elevated/10 transition-colors group">
                  <td className="p-4 text-center">
                    <button 
                      onClick={() => !m.isIdentified && setIdentifyingMovement(m)}
                      className={`transition-all ${m.isIdentified ? 'cursor-default' : 'hover:scale-110'}`}
                      title={m.isIdentified ? 'Identificado' : 'Haga clic para identificar entidad'}
                    >
                      {m.isIdentified ? (
                        <CheckCircle2 className="w-5 h-5 text-primary-green mx-auto" />
                      ) : (
                        <Circle className="w-5 h-5 text-primary-orange mx-auto" />
                      )}
                    </button>
                  </td>
                  <td className="p-4 text-xs font-mono text-text-secondary">{m.date.split('T')[0]}</td>
                  <td className="p-4">
                    <div className="flex items-center gap-2">
                      {m.amount > 0 ? (
                        <ArrowUpRight className="w-3 h-3 text-primary-green flex-shrink-0" />
                      ) : (
                        <ArrowDownLeft className="w-3 h-3 text-primary-orange flex-shrink-0" />
                      )}
                      {editingDescriptionId === m.id ? (
                        <div className="flex items-center gap-1 w-full">
                          <input 
                            type="text"
                            autoFocus
                            value={tempDescription}
                            onChange={(e) => setTempDescription(e.target.value)}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') handleUpdateDescription(m.id);
                              if (e.key === 'Escape') setEditingDescriptionId(null);
                            }}
                            onBlur={() => handleUpdateDescription(m.id)}
                            className="bg-background border border-primary-orange p-1 text-xs rounded-sm outline-none w-full"
                          />
                        </div>
                      ) : (
                        <div className="flex items-center gap-2 group/desc overflow-hidden">
                          <span className="text-xs font-medium truncate">{m.description}</span>
                          <button 
                            onClick={() => {
                              setEditingDescriptionId(m.id);
                              setTempDescription(m.description);
                            }}
                            className="p-1 text-text-secondary hover:text-primary-orange opacity-0 group-hover/desc:opacity-100 transition-all flex-shrink-0"
                            title="Editar descripción"
                          >
                            <FileEdit className="w-3 h-3" />
                          </button>
                        </div>
                      )}
                    </div>
                  </td>
                  <td className="p-4">
                    {entity ? (
                      <div className="flex flex-col">
                        <span className="text-xs font-bold">{entity.name}</span>
                        <span className="text-[9px] font-mono text-text-secondary uppercase">{entity.code}</span>
                      </div>
                    ) : (
                      <span className="text-[10px] font-mono text-text-secondary italic uppercase tracking-widest">Sin identificar</span>
                    )}
                  </td>
                  <td className={`p-4 numeric font-bold text-right ${m.amount >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                    {formatCurrency(m.amount)}
                  </td>
                  <td className="p-4 text-right">
                    <div className="flex items-center justify-end gap-1">
                      {m.amount > 0 && (
                        <button 
                          onClick={() => {
                            setReservingMovement(m);
                          }}
                          className="p-1.5 text-text-secondary hover:text-primary-orange transition-all"
                          title="Reservar Fondos"
                        >
                          <PiggyBank className="w-3.5 h-3.5" />
                        </button>
                      )}

                      {m.amount < 0 && (
                        <button 
                          onClick={() => setPayingFromSpaceMovement(m)}
                          className="p-1.5 text-text-secondary hover:text-primary-orange transition-all"
                          title="Pagar desde Espacio"
                        >
                          <Wallet className="w-3.5 h-3.5" />
                        </button>
                      )}

                      <button 
                        onClick={() => startEditingEntry(m)}
                        className="p-1.5 text-text-secondary hover:text-primary-orange transition-all"
                        title="Editar Asiento Completo"
                      >
                        <FileEdit className="w-3.5 h-3.5" />
                      </button>
                      
                      <button 
                        onClick={() => deleteBankMovement(m.id)}
                        className="p-1.5 text-text-secondary hover:text-primary-orange opacity-0 group-hover:opacity-100 transition-all"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
            {bankMovements.length === 0 && (
              <tr key="empty-bank">
                <td colSpan={6} className="p-12 text-center">
                  <div className="flex flex-col items-center gap-3 opacity-30">
                    <Search className="w-8 h-8" />
                    <p className="text-xs font-mono uppercase tracking-widest">No hay movimientos registrados</p>
                  </div>
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Import Modal */}
      {isImporting && (
        <ImportCSV onClose={() => setIsImporting(false)} />
      )}

      {/* Identify Modal */}
      {identifyingMovement && (
        <IdentifyModal 
          movement={identifyingMovement} 
          onClose={() => setIdentifyingMovement(null)} 
          getOrCreateEntry={getOrCreateEntry} 
        />
      )}

      {/* Reserve Modal */}
      {reservingMovement && (
        <ReserveModal 
          movement={reservingMovement} 
          onClose={() => setReservingMovement(null)} 
          getOrCreateEntry={getOrCreateEntry} 
          formatCurrency={formatCurrency}
        />
      )}

      {/* Pay from Space Modal */}
      {payingFromSpaceMovement && (
        <PayFromSpaceModal 
          movement={payingFromSpaceMovement} 
          onClose={() => setPayingFromSpaceMovement(null)} 
          getOrCreateEntry={getOrCreateEntry} 
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
