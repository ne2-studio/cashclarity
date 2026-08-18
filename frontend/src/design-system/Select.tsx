import type { SelectHTMLAttributes } from 'react';
import { cx } from './utils';

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  tone?: 'green' | 'orange';
}

export function Select({ tone = 'green', className, children, ...props }: SelectProps) {
  return (
    <select
      className={cx(
        'bg-background border border-border p-2 text-sm rounded-sm outline-none',
        tone === 'green' ? 'focus:ring-1 focus:ring-primary-green' : 'focus:ring-1 focus:ring-primary-orange',
        className,
      )}
      {...props}
    >
      {children}
    </select>
  );
}
