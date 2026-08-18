import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cx } from './utils';

interface IconButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode;
  hover?: 'default' | 'accent' | 'danger';
  size?: 'sm' | 'md';
}

const hoverClasses = {
  default: 'hover:text-text-primary hover:bg-surface-elevated',
  accent: 'hover:text-primary-orange hover:bg-primary-orange/10',
  danger: 'hover:text-primary-orange hover:bg-primary-orange/10',
};

const sizeClasses = {
  sm: 'p-1.5',
  md: 'p-2',
};

export function IconButton({
  children,
  hover = 'accent',
  size = 'sm',
  className,
  type = 'button',
  ...props
}: IconButtonProps) {
  return (
    <button
      type={type}
      className={cx(
        'inline-flex items-center justify-center text-text-secondary transition-all rounded-sm',
        sizeClasses[size],
        hoverClasses[hover],
        className,
      )}
      {...props}
    >
      {children}
    </button>
  );
}
