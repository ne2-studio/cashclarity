import { Button, Card, FormField, Input } from '../design-system';

export interface EntityDraft {
  code: string;
  name: string;
}

interface CreateEntityFormProps {
  entity: EntityDraft;
  onCancel: () => void;
  onChange: (entity: EntityDraft) => void;
  onSubmit: () => void;
}

export function CreateEntityForm({ entity, onCancel, onChange, onSubmit }: CreateEntityFormProps) {
  return (
    <Card className="p-6 animate-in fade-in slide-in-from-top-2 duration-300">
      <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-6">Crear Nueva Entidad</h3>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
        <FormField label="Código (4 dígitos)">
          <Input
            type="text"
            value={entity.code}
            onChange={e => onChange({ ...entity, code: e.target.value.replace(/\D/g, '').slice(0, 4) })}
            placeholder="Ej: 4300"
          />
        </FormField>
        <FormField label="Nombre">
          <Input
            type="text"
            value={entity.name}
            onChange={e => onChange({ ...entity, name: e.target.value })}
            placeholder="Ej: Cliente A"
          />
        </FormField>
      </div>
      <div className="flex justify-end gap-3">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button onClick={onSubmit}>
          Crear Entidad
        </Button>
      </div>
    </Card>
  );
}
