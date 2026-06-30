import React, { useState } from 'react';
import { X } from 'lucide-react';
import { useFinanceStore } from '../store/useFinanceStore';
import { Account, BankMovement, JournalLine, JournalEntry } from '../types';

interface IdentifyModalProps {
  movement: BankMovement;
  onClose: () => void;
  getOrCreateEntry: (movement: BankMovement) => Promise<JournalEntry | undefined>;
}

export function IdentifyModal({ movement, onClose, getOrCreateEntry }: IdentifyModalProps) {
  const { accounts, updateJournalEntry, updateBankMovement } = useFinanceStore();
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

    const newLines = entry.lines.map((l: JournalLine) => {
      if (l.accountId === uncategorizedAccount?.id) {
        return new JournalLine({ ...l, accountId: selectedEntityId });
      }
      return new JournalLine({ ...l });
    });

    await updateJournalEntry(entry.id, { lines: newLines });
    await updateBankMovement(movement.id, { 
      isIdentified: true, 
      entityId: selectedEntityId
    });

    onClose();
  };

  return (
    <div className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-surface border border-border w-full max-w-md rounded-sm shadow-2xl animate-in zoom-in-95 duration-200">
        <div className="p-6 border-b border-border flex items-center justify-between bg-surface-elevated/20">
          <h3 className="text-sm font-bold uppercase tracking-widest">Identificar Entidad</h3>
          <button onClick={onClose} className="text-text-secondary hover:text-text-primary">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="p-8 flex flex-col gap-6">
          <div className="flex flex-col gap-2">
            <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Seleccionar Contraparte</label>
            <select 
              value={selectedEntityId}
              onChange={e => setSelectedEntityId(e.target.value)}
              className="w-full bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
            >
              <option value="">Seleccionar entidad...</option>
              {entities.map((e: Account) => (
                <option key={e.id} value={e.id}>{e.code} - {e.name}</option>
              ))}
            </select>
          </div>
          <div className="flex justify-end gap-3">
            <button onClick={onClose} className="px-6 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary">Cancelar</button>
            <button onClick={handleIdentify} className="bg-primary-green text-background px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm">Identificar</button>
          </div>
        </div>
      </div>
    </div>
  );
}
