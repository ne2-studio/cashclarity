import type { HTMLAttributes } from 'react';
import { cx } from './utils';

interface CardProps extends HTMLAttributes<HTMLDivElement> {
  elevated?: boolean;
}

export function Card({ elevated = false, className, ...props }: CardProps) {
  return (
    <div
      className={cx(
        'bg-surface border border-border rounded-sm',
        elevated ? 'bg-surface-elevated/20' : '',
        className,
      )}
      {...props}
    />
  );
}
