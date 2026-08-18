import { useState } from 'react';
import {
  ArrowDownLeft,
  ArrowUpRight,
  CheckCircle2,
  Circle,
  FileEdit,
  PiggyBank,
  Search,
  Trash2,
  Wallet,
} from 'lucide-react';
import { Card, IconButton } from '../design-system';
import type { Account, BankMovement } from '../types';

interface MovementsTableProps {
  accounts: Account[];
  bankMovements: BankMovement[];
  onDeleteBankMovement: (id: string) => Promise<void>;
  onEditEntry: (movement: BankMovement) => void;
  onIdentifyMovement: (movement: BankMovement) => void;
  onPayFromSpace: (movement: BankMovement) => void;
  onReserveMovement: (movement: BankMovement) => void;
  onUpdateDescription: (id: string, description: string) => Promise<void>;
  formatCurrency: (value: number) => string;
}

export function MovementsTable({
  accounts,
  bankMovements,
  onDeleteBankMovement,
  onEditEntry,
  onIdentifyMovement,
  onPayFromSpace,
  onReserveMovement,
  onUpdateDescription,
  formatCurrency,
}: MovementsTableProps) {
  const [editingDescriptionId, setEditingDescriptionId] = useState<string | null>(null);
  const [tempDescription, setTempDescription] = useState('');

  const handleUpdateDescription = async (id: string) => {
    if (!tempDescription.trim()) {
      setEditingDescriptionId(null);
      return;
    }

    await onUpdateDescription(id, tempDescription);
    setEditingDescriptionId(null);
  };

  return (
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
          {bankMovements.map((m: BankMovement) => {
            const entity = accounts.find((a: Account) => a.id === m.entityId);

            return (
              <tr key={m.id} className="hover:bg-surface-elevated/10 transition-colors group">
                <td className="p-4 text-center">
                  <button
                    onClick={() => !m.isIdentified && onIdentifyMovement(m)}
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
                      <IconButton onClick={() => onReserveMovement(m)} title="Reservar Fondos">
                        <PiggyBank className="w-3.5 h-3.5" />
                      </IconButton>
                    )}
                    {m.amount < 0 && (
                      <IconButton onClick={() => onPayFromSpace(m)} title="Pagar desde Espacio">
                        <Wallet className="w-3.5 h-3.5" />
                      </IconButton>
                    )}
                    <IconButton onClick={() => onEditEntry(m)} title="Editar Asiento Completo">
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
  );
}
