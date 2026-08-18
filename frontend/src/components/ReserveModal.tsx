import React, { useState } from 'react';
import { X } from 'lucide-react';
import { Account, BankMovement, JournalEntry } from '../types';
import { appendReservationLines, validateReservations } from '../hooks/journalEntryLogic';
import { Button, Card, IconButton, Input, Modal, Select } from '../design-system';

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
    <Modal
      title="Reservar Fondos"
      onClose={onClose}
      width="lg"
      footer={(
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button onClick={handleReserve}>Guardar Reservas</Button>
        </>
      )}
    >
          <Card className="bg-background p-3 flex justify-between items-center">
            <span className="text-xs text-text-secondary uppercase font-mono">Disponible para reservar</span>
            <span className="text-sm font-bold text-primary-green">{formatCurrency(movement.amount)}</span>
          </Card>

          <div className="flex flex-col gap-4">
            {reservations.map((res, idx) => (
              <div key={idx} className="flex items-center gap-3">
                <Select
                  value={res.spaceId}
                  onChange={e => {
                    const newRes = [...reservations];
                    newRes[idx].spaceId = e.target.value;
                    setReservations(newRes);
                  }}
                  className="flex-1 text-xs"
                >
                  <option value="">Seleccionar espacio...</option>
                  {spaces.map((s: Account) => (
                    <option key={s.id} value={s.id}>{s.name}</option>
                  ))}
                </Select>
                <Input
                  type="number" 
                  value={res.amount}
                  onChange={e => {
                    const newRes = [...reservations];
                    newRes[idx].amount = parseFloat(e.target.value) || 0;
                    setReservations(newRes);
                  }}
                  className="w-24 text-xs font-mono"
                />
                <IconButton
                  onClick={() => setReservations(reservations.filter((_, i) => i !== idx))}
                  aria-label="Eliminar reserva"
                >
                  <X className="w-4 h-4" />
                </IconButton>
              </div>
            ))}
            <Button
              variant="link"
              onClick={() => setReservations([...reservations, { spaceId: '', amount: 0 }])}
            >
              + Añadir Reserva
            </Button>
          </div>
    </Modal>
  );
}
