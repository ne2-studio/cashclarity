import type { InputHTMLAttributes } from 'react';
import { cx } from './utils';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  tone?: 'green' | 'orange';
}

export function Input({ tone = 'green', className, ...props }: InputProps) {
  return (
    <input
      className={cx(
        'bg-background border border-border p-2 text-sm rounded-sm outline-none',
        tone === 'green' ? 'focus:ring-1 focus:ring-primary-green' : 'focus:ring-1 focus:ring-primary-orange',
        className,
      )}
      {...props}
    />
  );
}
