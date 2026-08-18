import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { cx } from './utils';

type ButtonVariant = 'primary' | 'secondary' | 'ghost' | 'link';
type ButtonSize = 'sm' | 'md';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: ButtonVariant;
  size?: ButtonSize;
  icon?: ReactNode;
}

const variantClasses: Record<ButtonVariant, string> = {
  primary: 'bg-primary-green text-background hover:bg-primary-green/90 rounded-sm',
  secondary: 'text-text-secondary border border-border hover:bg-surface-elevated rounded-sm',
  ghost: 'text-text-secondary hover:text-text-primary transition-all',
  link: 'text-primary-orange hover:underline self-start',
};

const sizeClasses: Record<ButtonSize, string> = {
  sm: 'px-4 py-1.5',
  md: 'px-6 py-2',
};

export function Button({
  variant = 'primary',
  size = 'md',
  icon,
  className,
  children,
  type = 'button',
  ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      className={cx(
        'inline-flex items-center justify-center gap-2 text-xs font-bold uppercase tracking-widest transition-all disabled:cursor-not-allowed disabled:bg-border disabled:text-text-secondary',
        variantClasses[variant],
        variant !== 'link' && sizeClasses[size],
        variant === 'link' && 'text-[10px]',
        className,
      )}
      {...props}
    >
      {icon}
      {children}
    </button>
  );
}
