import React from 'react';
import { X, AlertCircle, CheckCircle2 } from 'lucide-react';
import { Account, BankMovement, JournalLine } from '../types';

interface EditJournalEntryModalProps {
  movement: BankMovement;
  entry: {
    description: string;
    date: string;
    lines: JournalLine[];
  };
  accounts: Account[];
  onClose: () => void;
  onSave: (updatedEntry: {
    description: string;
    date: string;
    lines: JournalLine[];
  }) => void;
  setEntry: React.Dispatch<React.SetStateAction<{
    description: string;
    date: string;
    lines: JournalLine[];
  } | null>>;
  formatCurrency: (val: number) => string;
}

export function EditJournalEntryModal({
  movement,
  entry,
  accounts,
  onClose,
  onSave,
  setEntry,
  formatCurrency
}: EditJournalEntryModalProps) {
  const totalDebit = entry.lines.reduce((sum, l) => sum + l.debit, 0);
  const totalCredit = entry.lines.reduce((sum, l) => sum + l.credit, 0);
  const isUnbalanced = Math.abs(totalDebit - totalCredit) > 0.01;

  return (
    <div className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-surface border border-border w-full max-w-4xl max-h-[90vh] overflow-y-auto rounded-sm shadow-2xl animate-in zoom-in-95 duration-200">
        <div className="p-6 border-b border-border flex items-center justify-between bg-surface-elevated/20 sticky top-0 z-10">
          <div className="flex flex-col gap-1">
            <h3 className="text-sm font-bold uppercase tracking-widest text-text-primary">Edición de Asiento Contable</h3>
            <p className="text-[10px] font-mono text-text-secondary">Ajustando registro para: {movement.description}</p>
          </div>
          <button onClick={onClose} className="text-text-secondary hover:text-text-primary">
            <X className="w-5 h-5" />
          </button>
        </div>

        <div className="p-8">
          {/* Movement Summary */}
          <div className="bg-background border border-border p-4 mb-8 flex items-center justify-between rounded-sm">
            <div className="flex flex-col gap-1">
              <span className="text-[9px] font-mono uppercase text-text-secondary">Movimiento Bancario</span>
              <span className="text-sm font-medium">{movement.description}</span>
            </div>
            <div className="text-right">
              <span className="text-[9px] font-mono uppercase text-text-secondary">Importe</span>
              <p className={`text-lg font-bold numeric ${movement.amount >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                {formatCurrency(movement.amount)}
              </p>
            </div>
          </div>

          {/* Entry Editor */}
          <div className="flex flex-col gap-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div className="flex flex-col gap-2">
                <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Descripción del Asiento</label>
                <input 
                  type="text" 
                  value={entry.description}
                  onChange={e => setEntry(prev => prev ? ({ ...prev, description: e.target.value }) : null)}
                  className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-orange outline-none"
                />
              </div>
              <div className="flex flex-col gap-2">
                <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Fecha Contable</label>
                <input 
                  type="date" 
                  value={entry.date}
                  onChange={e => setEntry(prev => prev ? ({ ...prev, date: e.target.value }) : null)}
                  className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-orange outline-none"
                />
              </div>
            </div>

            <div className="bg-background border border-border rounded-sm overflow-hidden">
              <table className="w-full text-left">
                <thead>
                  <tr className="bg-surface-elevated/10 border-b border-border">
                    <th className="p-3 text-[9px] font-mono uppercase tracking-widest text-text-secondary">Cuenta</th>
                    <th className="p-3 text-[9px] font-mono uppercase tracking-widest text-text-secondary text-right">Debe</th>
                    <th className="p-3 text-[9px] font-mono uppercase tracking-widest text-text-secondary text-right">Haber</th>
                    <th className="p-3 text-[9px] font-mono uppercase tracking-widest text-text-secondary w-12"></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/30">
                  {entry.lines.map((line, idx) => (
                    <tr key={line.id || idx}>
                      <td className="p-2">
                        <select 
                          value={line.accountId}
                          onChange={e => {
                            const newLines = [...entry.lines];
                            newLines[idx].accountId = e.target.value;
                            setEntry({ ...entry, lines: newLines });
                          }}
                          className="w-full bg-transparent border-none text-xs focus:ring-0 outline-none"
                        >
                          {accounts.map((a: Account) => (
                            <option key={a.id} value={a.id}>{a.code} - {a.name}</option>
                          ))}
                        </select>
                      </td>
                      <td className="p-2">
                        <input 
                          type="number" 
                          value={line.debit || ''}
                          onChange={e => {
                            const newLines = [...entry.lines];
                            newLines[idx].debit = parseFloat(e.target.value) || 0;
                            setEntry({ ...entry, lines: newLines });
                          }}
                          placeholder="0.00"
                          className="w-full bg-transparent border-none text-xs text-right focus:ring-0 outline-none font-mono"
                        />
                      </td>
                      <td className="p-2">
                        <input 
                          type="number" 
                          value={line.credit || ''}
                          onChange={e => {
                            const newLines = [...entry.lines];
                            newLines[idx].credit = parseFloat(e.target.value) || 0;
                            setEntry({ ...entry, lines: newLines });
                          }}
                          placeholder="0.00"
                          className="w-full bg-transparent border-none text-xs text-right focus:ring-0 outline-none font-mono"
                        />
                      </td>
                      <td className="p-2 text-center">
                        <button 
                          onClick={() => {
                            const newLines = entry.lines.filter((_, i) => i !== idx);
                            setEntry({ ...entry, lines: newLines });
                          }}
                          className="text-text-secondary hover:text-primary-orange"
                        >
                          <X className="w-3 h-3" />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="bg-surface-elevated/5 border-t border-border">
                    <td className="p-3">
                      <button 
                        onClick={() => {
                          setEntry({
                            ...entry,
                            lines: [...entry.lines, new JournalLine({ id: crypto.randomUUID(), accountId: accounts[0]?.id || '', debit: 0, credit: 0 })]
                          });
                        }}
                        className="text-[9px] font-bold uppercase tracking-widest text-primary-orange hover:underline"
                      >
                        + Añadir Línea
                      </button>
                    </td>
                    <td className="p-3 text-right font-mono text-xs font-bold">
                      {formatCurrency(totalDebit)}
                    </td>
                    <td className="p-3 text-right font-mono text-xs font-bold">
                      {formatCurrency(totalCredit)}
                    </td>
                    <td></td>
                  </tr>
                </tfoot>
              </table>
            </div>

            <div className="flex items-center justify-between">
              {isUnbalanced ? (
                <div className="flex items-center gap-2 text-primary-orange animate-pulse">
                  <AlertCircle className="w-4 h-4" />
                  <span className="text-[10px] font-bold uppercase tracking-widest">Asiento Descuadrado</span>
                </div>
              ) : (
                <div className="flex items-center gap-2 text-primary-green">
                  <CheckCircle2 className="w-4 h-4" />
                  <span className="text-[10px] font-bold uppercase tracking-widest">Asiento Cuadrado</span>
                </div>
              )}

              <div className="flex gap-3">
                <button 
                  onClick={onClose}
                  className="px-6 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary hover:text-text-primary transition-all"
                >
                  Cancelar
                </button>
                <button 
                  onClick={() => onSave(entry)}
                  className="bg-primary-green text-background px-8 py-2 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all shadow-lg"
                >
                  Confirmar y Guardar
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
