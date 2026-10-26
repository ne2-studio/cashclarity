import { useState, type FormEvent } from 'react';
import { Button, FormField, Input } from '../design-system';

interface CreateWorkspaceFormProps {
  onSubmit: (name: string) => Promise<void>;
  onCancel?: () => void;
  submitLabel?: string;
}

export const WORKSPACE_NAME_MAX_LENGTH = 100;

export function CreateWorkspaceForm({ onSubmit, onCancel, submitLabel = 'Crear workspace' }: CreateWorkspaceFormProps) {
  const [name, setName] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) {
      setError('El nombre es obligatorio');
      return;
    }
    setIsSubmitting(true);
    setError(null);
    try {
      await onSubmit(trimmed);
    } catch (e) {
      setError((e as Error).message);
      setIsSubmitting(false);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      <FormField label="Nombre del workspace">
        <Input
          type="text"
          autoFocus
          value={name}
          maxLength={WORKSPACE_NAME_MAX_LENGTH}
          onChange={(e) => setName(e.target.value)}
          placeholder="Ej: Cuenta Sabadell"
          aria-label="Nombre del workspace"
        />
        {error && <p role="alert" className="text-xs font-mono text-primary-orange">{error}</p>}
      </FormField>
      <div className="flex justify-end gap-3">
        {onCancel && (
          <Button variant="ghost" onClick={onCancel}>
            Cancelar
          </Button>
        )}
        <Button type="submit" disabled={isSubmitting}>
          {submitLabel}
        </Button>
      </div>
    </form>
  );
}
