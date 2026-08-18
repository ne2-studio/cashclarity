import type { ReactNode } from 'react';

interface FormFieldProps {
  label: string;
  children: ReactNode;
}

export function FormField({ label, children }: FormFieldProps) {
  return (
    <div className="flex flex-col gap-2">
      <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">{label}</label>
      {children}
    </div>
  );
}
