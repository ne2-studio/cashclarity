import React, { useState } from 'react';
import { X } from 'lucide-react';
import { useFinanceStore } from '../store/useFinanceStore';
import { Account, BankMovement, JournalLine, JournalEntry } from '../types';

interface PayFromSpaceModalProps {
  movement: BankMovement;
  onClose: () => void;
  getOrCreateEntry: (movement: BankMovement) => Promise<JournalEntry | undefined>;
}

export function PayFromSpaceModal({ movement, onClose, getOrCreateEntry }: PayFromSpaceModalProps) {
  const { accounts, updateJournalEntry } = useFinanceStore();
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

    const newLines = entry.lines.map((l: JournalLine) => {
      if (l.accountId === mainAccount.id) {
        return new JournalLine({ ...l, accountId: selectedSpaceId });
      }
      return new JournalLine({ ...l });
    });

    await updateJournalEntry(entry.id, { lines: newLines });
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-surface border border-border w-full max-w-md rounded-sm shadow-2xl animate-in zoom-in-95 duration-200">
        <div className="p-6 border-b border-border flex items-center justify-between bg-surface-elevated/20">
          <h3 className="text-sm font-bold uppercase tracking-widest">Pagar desde Espacio</h3>
          <button onClick={onClose} className="text-text-secondary hover:text-text-primary">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="p-8 flex flex-col gap-6">
          <div className="flex flex-col gap-2">
            <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Seleccionar Espacio de Origen</label>
            <select 
              value={selectedSpaceId}
              onChange={e => setSelectedSpaceId(e.target.value)}
              className="w-full bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
            >
              <option value="">Seleccionar espacio...</option>
              {spaces.map((s: Account) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>
          <div className="flex justify-end gap-3">
            <button onClick={onClose} className="px-6 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary">Cancelar</button>
            <button onClick={handlePayFromSpace} className="bg-primary-green text-background px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm">Confirmar Pago</button>
          </div>
        </div>
      </div>
    </div>
  );
}
