import React, { useState, useMemo } from 'react';
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
  ArrowUpRight,
  ArrowDownLeft,
  Search,
  Trash2,
  FileEdit,
  PiggyBank,
  Wallet
} from 'lucide-react';
import { Account, BankMovement, JournalEntry, JournalLine } from '../types';
import {
  createEntryFromMovement,
  toEditableJournalEntry,
  validateJournalEntry,
} from '../hooks/journalEntryLogic';
import { Button, Card, FormField, IconButton, Input } from '../design-system';

interface BankStatementProps {
  accounts: Account[];
  bankMovements: BankMovement[];
  journalEntries: JournalEntry[];
  onAddBankMovement: (movement: Omit<BankMovement, 'id' | 'isIdentified'>) => Promise<BankMovement>;
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
  onUpdateBankMovement,
  onDeleteBankMovement,
  onAddJournalEntry,
  onUpdateJournalEntry,
}: BankStatementProps) {

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

  const handleUpdateDescription = async (id: string) => {
    if (!tempDescription.trim()) {
      setEditingDescriptionId(null);
      return;
    }
    
    const movement = bankMovements.find(m => m.id === id);
    if (!movement) return;

    await onUpdateBankMovement(id, { description: tempDescription });

    if (movement.journalEntryId) {
      await onUpdateJournalEntry(movement.journalEntryId, { description: tempDescription });
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
          <Button
            variant="secondary"
            onClick={handleImportCSV}
            icon={<Upload className="w-3.5 h-3.5" />}
          >
            Importar CSV
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
        <Card className="p-6 animate-in fade-in slide-in-from-top-2 duration-300">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary">Registrar Movimiento Manual</h3>
            <IconButton onClick={() => setIsAdding(false)} hover="default" aria-label="Cerrar formulario">
              <X className="w-4 h-4" />
            </IconButton>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <FormField label="Fecha">
              <Input
                type="date" 
                value={newMovement.date}
                onChange={e => setNewMovement(prev => ({ ...prev, date: e.target.value }))}
                tone="orange"
              />
            </FormField>
            <FormField label="Concepto">
              <Input
                type="text" 
                value={newMovement.description}
                onChange={e => setNewMovement(prev => ({ ...prev, description: e.target.value }))}
                placeholder="Ej: Transferencia Recibida"
                tone="orange"
              />
            </FormField>
            <FormField label="Importe (+ Ingreso, - Gasto)">
              <Input
                type="number" 
                step="0.01"
                value={newMovement.amount}
                onChange={e => setNewMovement(prev => ({ ...prev, amount: e.target.value }))}
                placeholder="0.00"
                tone="orange"
                className="font-mono"
              />
            </FormField>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setIsAdding(false)}>
              Cancelar
            </Button>
            <Button onClick={handleAddMovement}>
              Guardar Movimiento
            </Button>
          </div>
        </Card>
      )}

      {/* Movements Table */}
      <Card className="overflow-hidden">
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
                          <IconButton
                            onClick={() => {
                              setEditingDescriptionId(m.id);
                              setTempDescription(m.description);
                            }}
                            className="p-1 opacity-0 group-hover/desc:opacity-100 flex-shrink-0"
                            title="Editar descripción"
                          >
                            <FileEdit className="w-3 h-3" />
                          </IconButton>
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
                        <IconButton
                          onClick={() => {
                            setReservingMovement(m);
                          }}
                          title="Reservar Fondos"
                        >
                          <PiggyBank className="w-3.5 h-3.5" />
                        </IconButton>
                      )}

                      {m.amount < 0 && (
                        <IconButton
                          onClick={() => setPayingFromSpaceMovement(m)}
                          title="Pagar desde Espacio"
                        >
                          <Wallet className="w-3.5 h-3.5" />
                        </IconButton>
                      )}

                      <IconButton
                        onClick={() => startEditingEntry(m)}
                        title="Editar Asiento Completo"
                      >
                        <FileEdit className="w-3.5 h-3.5" />
                      </IconButton>
                      
                      <IconButton
                        onClick={() => onDeleteBankMovement(m.id)}
                        className="opacity-0 group-hover:opacity-100"
                        title="Eliminar movimiento"
                      >
                        <Trash2 className="w-3.5 h-3.5" />
                      </IconButton>
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
      </Card>

      {/* Import Modal */}
      {isImporting && (
        <ImportCSV onClose={() => setIsImporting(false)} onAddBankMovement={onAddBankMovement} />
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
