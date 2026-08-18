import React, { useState } from 'react';
import { Account, BankMovement, JournalEntry } from '../types';
import { payEntryFromSpaceLines } from '../hooks/journalEntryLogic';
import { Button, FormField, Modal, Select } from '../design-system';

interface PayFromSpaceModalProps {
  movement: BankMovement;
  accounts: Account[];
  onClose: () => void;
  getOrCreateEntry: (movement: BankMovement) => Promise<JournalEntry | undefined>;
  onUpdateJournalEntry: (id: string, updates: Partial<JournalEntry>) => Promise<void>;
}

export function PayFromSpaceModal({
  movement,
  accounts,
  onClose,
  getOrCreateEntry,
  onUpdateJournalEntry,
}: PayFromSpaceModalProps) {
  const [selectedSpaceId, setSelectedSpaceId] = useState('');

  const spaces = accounts.filter((a: Account) => a.type === 'space');
  const mainAccount = accounts.find((a: Account) => a.type === 'main');

  const handlePayFromSpace = async () => {
    if (!selectedSpaceId) {
      alert('Por favor, selecciona un espacio');
      return;
    }
    
    if (!mainAccount) {
      alert('No se ha encontrado la cuenta principal');
      return;
    }

    const entry = await getOrCreateEntry(movement);
    if (!entry) return;

    const newLines = payEntryFromSpaceLines(entry.lines, mainAccount.id, selectedSpaceId);

    await onUpdateJournalEntry(entry.id, { lines: newLines });
    onClose();
  };

  return (
    <Modal
      title="Pagar desde Espacio"
      onClose={onClose}
      footer={(
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button onClick={handlePayFromSpace}>Confirmar Pago</Button>
        </>
      )}
    >
          <FormField label="Seleccionar Espacio de Origen">
            <Select
              value={selectedSpaceId}
              onChange={e => setSelectedSpaceId(e.target.value)}
              className="w-full"
            >
              <option value="">Seleccionar espacio...</option>
              {spaces.map((s: Account) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </Select>
          </FormField>
    </Modal>
  );
}
