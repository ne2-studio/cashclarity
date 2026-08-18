import type { InputHTMLAttributes } from 'react';
import { Search } from 'lucide-react';
import { Input } from './Input';
import { cx } from './utils';

export function SearchField({ className, ...props }: InputHTMLAttributes<HTMLInputElement>) {
  return (
    <div className={cx('relative', className)}>
      <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-secondary" />
      <Input className="w-full pl-10 pr-4" {...props} />
    </div>
  );
}
