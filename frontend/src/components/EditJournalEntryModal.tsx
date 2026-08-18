import React from 'react';
import { X, AlertCircle, CheckCircle2 } from 'lucide-react';
import { Account, BankMovement, JournalLine } from '../types';
import { getJournalTotals } from '../hooks/journalEntryLogic';
import { Button, Card, FormField, IconButton, Input, Modal, Select } from '../design-system';

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
  const { debit: totalDebit, credit: totalCredit } = getJournalTotals(entry.lines);
  const isUnbalanced = Math.abs(totalDebit - totalCredit) > 0.01;

  return (
    <Modal
      title="Edición de Asiento Contable"
      subtitle={`Ajustando registro para: ${movement.description}`}
      onClose={onClose}
      width="4xl"
      scrollable
    >
          {/* Movement Summary */}
          <Card className="bg-background p-4 flex items-center justify-between">
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
          </Card>

          {/* Entry Editor */}
          <div className="flex flex-col gap-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <FormField label="Descripción del Asiento">
                <Input
                  type="text" 
                  value={entry.description}
                  onChange={e => setEntry(prev => prev ? ({ ...prev, description: e.target.value }) : null)}
                  tone="orange"
                />
              </FormField>
              <FormField label="Fecha Contable">
                <Input
                  type="date" 
                  value={entry.date}
                  onChange={e => setEntry(prev => prev ? ({ ...prev, date: e.target.value }) : null)}
                  tone="orange"
                />
              </FormField>
            </div>

            <Card className="bg-background overflow-x-auto">
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
                        <Select
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
                        </Select>
                      </td>
                      <td className="p-2">
                        <Input
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
                        <Input
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
                        <IconButton
                          onClick={() => {
                            const newLines = entry.lines.filter((_, i) => i !== idx);
                            setEntry({ ...entry, lines: newLines });
                          }}
                          aria-label="Eliminar línea"
                        >
                          <X className="w-3 h-3" />
                        </IconButton>
                      </td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr className="bg-surface-elevated/5 border-t border-border">
                    <td className="p-3">
                      <Button
                        variant="link"
                        onClick={() => {
                          setEntry({
                            ...entry,
                            lines: [...entry.lines, new JournalLine({ id: crypto.randomUUID(), accountId: accounts[0]?.id || '', debit: 0, credit: 0 })]
                          });
                        }}
                        className="text-[9px]"
                      >
                        + Añadir Línea
                      </Button>
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
            </Card>

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
                <Button variant="ghost" onClick={onClose}>
                  Cancelar
                </Button>
                <Button
                  onClick={() => onSave(entry)}
                  className="px-8 shadow-lg"
                >
                  Confirmar y Guardar
                </Button>
              </div>
            </div>
          </div>
    </Modal>
  );
}
