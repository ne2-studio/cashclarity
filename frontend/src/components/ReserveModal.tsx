import React, { useState } from 'react';
import { X } from 'lucide-react';
import { Account, BankMovement, JournalEntry } from '../types';
import { appendReservationLines, validateReservations } from '../hooks/journalEntryLogic';

interface ReserveModalProps {
  movement: BankMovement;
  accounts: Account[];
  onClose: () => void;
  getOrCreateEntry: (movement: BankMovement) => Promise<JournalEntry | undefined>;
  onUpdateJournalEntry: (id: string, updates: Partial<JournalEntry>) => Promise<void>;
  formatCurrency: (val: number) => string;
}

export function ReserveModal({
  movement,
  accounts,
  onClose,
  getOrCreateEntry,
  onUpdateJournalEntry,
  formatCurrency,
}: ReserveModalProps) {
  const [reservations, setReservations] = useState<{ spaceId: string, amount: number }[]>([]);

  const spaces = accounts.filter((a: Account) => a.type === 'space');
  const mainAccount = accounts.find((a: Account) => a.type === 'main');

  const handleReserve = async () => {
    if (reservations.length === 0) return;
    
    const validationError = validateReservations(reservations, movement.amount, Boolean(mainAccount));
    if (validationError) {
      alert(validationError);
      return;
    }

    const entry = await getOrCreateEntry(movement);
    if (!entry) return;

    const newLines = appendReservationLines(entry.lines, reservations, mainAccount!.id);

    await onUpdateJournalEntry(entry.id, { lines: newLines });
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-surface border border-border w-full max-w-lg rounded-sm shadow-2xl animate-in zoom-in-95 duration-200">
        <div className="p-6 border-b border-border flex items-center justify-between bg-surface-elevated/20">
          <h3 className="text-sm font-bold uppercase tracking-widest">Reservar Fondos</h3>
          <button onClick={onClose} className="text-text-secondary hover:text-text-primary">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="p-8 flex flex-col gap-6">
          <div className="bg-background border border-border p-3 rounded-sm flex justify-between items-center">
            <span className="text-xs text-text-secondary uppercase font-mono">Disponible para reservar</span>
            <span className="text-sm font-bold text-primary-green">{formatCurrency(movement.amount)}</span>
          </div>

          <div className="flex flex-col gap-4">
            {reservations.map((res, idx) => (
              <div key={idx} className="flex items-center gap-3">
                <select 
                  value={res.spaceId}
                  onChange={e => {
                    const newRes = [...reservations];
                    newRes[idx].spaceId = e.target.value;
                    setReservations(newRes);
                  }}
                  className="flex-1 bg-background border border-border p-2 text-xs rounded-sm outline-none"
                >
                  <option value="">Seleccionar espacio...</option>
                  {spaces.map((s: Account) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </select>
                <input 
                  type="number" 
                  value={res.amount}
                  onChange={e => {
                    const newRes = [...reservations];
                    newRes[idx].amount = parseFloat(e.target.value) || 0;
                    setReservations(newRes);
                  }}
                  className="w-24 bg-background border border-border p-2 text-xs rounded-sm outline-none font-mono"
                />
                <button 
                  onClick={() => setReservations(reservations.filter((_, i) => i !== idx))}
                  className="text-text-secondary hover:text-primary-orange"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>
            ))}
            <button 
              onClick={() => setReservations([...reservations, { spaceId: '', amount: 0 }])}
              className="text-[10px] font-bold uppercase tracking-widest text-primary-orange hover:underline self-start"
            >
              + Añadir Reserva
            </button>
          </div>

          <div className="flex justify-end gap-3 pt-4 border-t border-border">
            <button onClick={onClose} className="px-6 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary">Cancelar</button>
            <button onClick={handleReserve} className="bg-primary-green text-background px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm">Guardar Reservas</button>
          </div>
        </div>
      </div>
    </div>
  );
}
