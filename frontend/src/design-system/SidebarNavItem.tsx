import type { ElementType } from 'react';
import { NavLink } from 'react-router-dom';
import { cx } from './utils';

interface SidebarNavItemProps {
  to: string;
  icon: ElementType<{ className?: string }>;
  children: string;
}

export function SidebarNavItem({ to, icon: Icon, children }: SidebarNavItemProps) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) => cx(
        'w-full flex items-center gap-3 px-6 py-2.5 text-sm font-medium transition-all duration-200 border-r-2',
        isActive
          ? 'bg-surface-elevated text-text-primary border-primary-orange'
          : 'text-text-secondary border-transparent hover:text-text-primary hover:bg-surface-elevated/50',
      )}
    >
      {({ isActive }) => (
        <>
          <Icon className={cx('w-4 h-4', isActive && 'text-primary-orange')} />
          {children}
        </>
      )}
    </NavLink>
  );
}
