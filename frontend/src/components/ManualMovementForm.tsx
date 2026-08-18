import { X } from 'lucide-react';
import { Button, Card, FormField, IconButton, Input } from '../design-system';
import type { BankMovement } from '../types';

export type ManualMovementDraft = Pick<BankMovement, 'date' | 'description'> & {
  amount: string;
};

interface ManualMovementFormProps {
  movement: ManualMovementDraft;
  onChange: (movement: ManualMovementDraft) => void;
  onCancel: () => void;
  onSubmit: () => void;
}

export function ManualMovementForm({ movement, onChange, onCancel, onSubmit }: ManualMovementFormProps) {
  return (
    <Card className="p-6 animate-in fade-in slide-in-from-top-2 duration-300">
      <div className="flex items-center justify-between mb-6">
        <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary">Registrar Movimiento Manual</h3>
        <IconButton onClick={onCancel} hover="default" aria-label="Cerrar formulario">
          <X className="w-4 h-4" />
        </IconButton>
      </div>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <FormField label="Fecha">
          <Input
            type="date"
            value={movement.date}
            onChange={e => onChange({ ...movement, date: e.target.value })}
            tone="orange"
          />
        </FormField>
        <FormField label="Concepto">
          <Input
            type="text"
            value={movement.description}
            onChange={e => onChange({ ...movement, description: e.target.value })}
            placeholder="Ej: Transferencia Recibida"
            tone="orange"
          />
        </FormField>
        <FormField label="Importe (+ Ingreso, - Gasto)">
          <Input
            type="number"
            step="0.01"
            value={movement.amount}
            onChange={e => onChange({ ...movement, amount: e.target.value })}
            placeholder="0.00"
            tone="orange"
            className="font-mono"
          />
        </FormField>
      </div>
      <div className="flex justify-end gap-3">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button onClick={onSubmit}>
          Guardar Movimiento
        </Button>
      </div>
    </Card>
  );
}
