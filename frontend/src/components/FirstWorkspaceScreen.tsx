import { ShieldCheck } from 'lucide-react';
import { Card } from '../design-system';
import { CreateWorkspaceForm } from './CreateWorkspaceForm';

interface FirstWorkspaceScreenProps {
  onCreate: (name: string) => Promise<void>;
}

export function FirstWorkspaceScreen({ onCreate }: FirstWorkspaceScreenProps) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-background p-6">
      <Card className="w-full max-w-md p-8 flex flex-col gap-6">
        <div className="flex items-center gap-3">
          <ShieldCheck className="w-6 h-6 text-primary-orange" />
          <span className="text-lg font-bold tracking-tight uppercase font-mono">CashClarity</span>
        </div>
        <div className="flex flex-col gap-2">
          <h1 className="text-sm font-bold uppercase tracking-widest text-text-primary">Crea tu primer workspace</h1>
          <p className="text-xs text-text-secondary leading-relaxed">
            Un workspace representa un extracto bancario con su propia contabilidad. Dale un nombre para empezar.
          </p>
        </div>
        <CreateWorkspaceForm onSubmit={onCreate} />
      </Card>
    </div>
  );
}
