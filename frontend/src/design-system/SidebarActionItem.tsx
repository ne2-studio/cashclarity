import type { ButtonHTMLAttributes, ElementType } from 'react';
import { cx } from './utils';

interface SidebarActionItemProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  icon: ElementType<{ className?: string }>;
}

export function SidebarActionItem({
  icon: Icon,
  children,
  className,
  type = 'button',
  ...props
}: SidebarActionItemProps) {
  return (
    <button
      type={type}
      className={cx(
        'w-full flex items-center gap-3 px-6 py-2.5 text-sm font-medium text-text-secondary hover:text-primary-orange hover:bg-surface-elevated/50 transition-all duration-200 rounded-sm',
        className,
      )}
      {...props}
    >
      <Icon className="w-4 h-4" />
      {children}
    </button>
  );
}
