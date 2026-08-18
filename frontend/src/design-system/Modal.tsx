import type { ReactNode } from 'react';
import { X } from 'lucide-react';
import { IconButton } from './IconButton';
import { cx } from './utils';

type ModalWidth = 'md' | 'lg' | '2xl' | '4xl';

interface ModalProps {
  title: string;
  subtitle?: ReactNode;
  onClose: () => void;
  children: ReactNode;
  footer?: ReactNode;
  width?: ModalWidth;
  scrollable?: boolean;
}

const widthClasses: Record<ModalWidth, string> = {
  md: 'max-w-md',
  lg: 'max-w-lg',
  '2xl': 'max-w-2xl',
  '4xl': 'max-w-4xl',
};

export function Modal({
  title,
  subtitle,
  onClose,
  children,
  footer,
  width = 'md',
  scrollable = false,
}: ModalProps) {
  return (
    <div className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className={cx('bg-surface border border-border w-full rounded-sm shadow-2xl animate-in zoom-in-95 duration-200', widthClasses[width], scrollable && 'max-h-[90vh] overflow-y-auto')}>
        <div className={cx('p-6 border-b border-border flex items-center justify-between bg-surface-elevated/20', scrollable && 'sticky top-0 z-10')}>
          <div className="flex flex-col gap-1">
            <h3 className="text-sm font-bold uppercase tracking-widest text-text-primary">{title}</h3>
            {subtitle && <p className="text-[10px] font-mono text-text-secondary">{subtitle}</p>}
          </div>
          <IconButton onClick={onClose} hover="default" size="sm" aria-label="Cerrar">
            <X className="w-5 h-5" />
          </IconButton>
        </div>
        <div className="p-8 flex flex-col gap-6">
          {children}
          {footer && <div className="flex justify-end gap-3 pt-4 border-t border-border">{footer}</div>}
        </div>
      </div>
    </div>
  );
}
