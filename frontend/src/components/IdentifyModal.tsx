import React, { useState } from 'react';
import { Account, BankMovement, JournalEntry } from '../types';
import { identifyEntryLines } from '../hooks/journalEntryLogic';
import { Button, FormField, Modal, Select } from '../design-system';

interface IdentifyModalProps {
  movement: BankMovement;
  accounts: Account[];
  onClose: () => void;
  getOrCreateEntry: (movement: BankMovement) => Promise<JournalEntry | undefined>;
  onUpdateJournalEntry: (id: string, updates: Partial<JournalEntry>) => Promise<void>;
  onUpdateBankMovement: (id: string, updates: Partial<BankMovement>) => Promise<void>;
}

export function IdentifyModal({
  movement,
  accounts,
  onClose,
  getOrCreateEntry,
  onUpdateJournalEntry,
  onUpdateBankMovement,
}: IdentifyModalProps) {
  const [selectedEntityId, setSelectedEntityId] = useState('');

  const entities = accounts.filter((a: Account) => a.type === 'entity');
  const uncategorizedAccount = accounts.find((a: Account) => a.type === 'uncategorized');

  const handleIdentify = async () => {
    if (!selectedEntityId) {
      alert('Por favor, selecciona una entidad');
      return;
    }
    
    const entry = await getOrCreateEntry(movement);
    if (!entry) return;

    const newLines = identifyEntryLines(entry.lines, uncategorizedAccount?.id, selectedEntityId);

    await onUpdateJournalEntry(entry.id, { lines: newLines });
    await onUpdateBankMovement(movement.id, { 
      isIdentified: true, 
      entityId: selectedEntityId
    });

    onClose();
  };

  return (
    <Modal
      title="Identificar Entidad"
      onClose={onClose}
      footer={(
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button onClick={handleIdentify}>Identificar</Button>
        </>
      )}
    >
          <FormField label="Seleccionar Contraparte">
            <Select
              value={selectedEntityId}
              onChange={e => setSelectedEntityId(e.target.value)}
              className="w-full"
            >
              <option value="">Seleccionar entidad...</option>
              {entities.map((e: Account) => (
                <option key={e.id} value={e.id}>{e.code} - {e.name}</option>
              ))}
            </Select>
          </FormField>
    </Modal>
  );
}
