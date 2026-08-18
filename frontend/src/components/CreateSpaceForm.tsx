import { Button, Card, FormField, Input } from '../design-system';

export interface SpaceDraft {
  code: string;
  name: string;
}

interface CreateSpaceFormProps {
  space: SpaceDraft;
  onCancel: () => void;
  onChange: (space: SpaceDraft) => void;
  onSubmit: () => void;
}

export function CreateSpaceForm({ space, onCancel, onChange, onSubmit }: CreateSpaceFormProps) {
  return (
    <Card className="p-6 animate-in fade-in slide-in-from-top-2 duration-300">
      <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-6">Crear Nuevo Espacio</h3>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-8">
        <FormField label="Código (4 dígitos)">
          <Input
            type="text"
            value={space.code}
            onChange={e => onChange({ ...space, code: e.target.value.replace(/\D/g, '').slice(0, 4) })}
            placeholder="Ej: 5722"
          />
        </FormField>
        <FormField label="Nombre">
          <Input
            type="text"
            value={space.name}
            onChange={e => onChange({ ...space, name: e.target.value })}
            placeholder="Ej: Reserva IVA"
          />
        </FormField>
      </div>
      <div className="flex justify-end gap-3">
        <Button variant="ghost" onClick={onCancel}>
          Cancelar
        </Button>
        <Button onClick={onSubmit}>
          Crear Espacio
        </Button>
      </div>
    </Card>
  );
}
