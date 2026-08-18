import { Button, Card, FormField, Input, Select } from '../design-system';
import type { AccountType } from '../types';

export interface AccountDraft {
  code: string;
  name: string;
  type: AccountType;
}

interface CreateAccountFormProps {
  account: AccountDraft;
  onCancel: () => void;
  onChange: (account: AccountDraft) => void;
  onSubmit: () => void;
}

export function CreateAccountForm({ account, onCancel, onChange, onSubmit }: CreateAccountFormProps) {
  return (
    <Card className="p-6 animate-in fade-in slide-in-from-top-2 duration-300">
      <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-6">Crear Nueva Cuenta</h3>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
        <FormField label="Código (4 dígitos)">
          <Input
            type="text"
            value={account.code}
            onChange={e => onChange({ ...account, code: e.target.value.replace(/\D/g, '').slice(0, 4) })}
            placeholder="Ej: 5721"
          />
        </FormField>
        <FormField label="Nombre">
          <Input
            type="text"
            value={account.name}
            onChange={e => onChange({ ...account, name: e.target.value })}
            placeholder="Ej: Banco Sabadell"
          />
        </FormField>
        <FormField label="Tipo">
          <Select
            value={account.type}
            onChange={e => onChange({ ...account, type: e.target.value as AccountType })}
          >
            <option value="space">ESPACIO</option>
            <option value="entity">ENTIDAD</option>
          </Select>
        </FormField>
      </div>
      <div className="flex justify-end gap-3">
        <Button variant="ghost" onClick={onCancel}>Cancelar</Button>
        <Button onClick={onSubmit}>Crear Cuenta</Button>
      </div>
    </Card>
  );
}
