import type { ReactNode } from 'react';

interface PageHeaderProps {
  icon: ReactNode;
  title: string;
  subtitle: string;
  actions?: ReactNode;
}

export function PageHeader({ icon, title, subtitle, actions }: PageHeaderProps) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex flex-col gap-1">
        <h2 className="text-lg font-medium tracking-tight text-text-primary flex items-center gap-2">
          {icon}
          {title}
        </h2>
        <p className="text-xs text-text-secondary">{subtitle}</p>
      </div>
      {actions}
    </div>
  );
}
