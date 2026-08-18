import React, { useState } from 'react';
import {
  LayoutDashboard,
  PiggyBank,
  Users,
  ShieldCheck,
  BookOpen,
  ListTree,
  Landmark,
  LogOut,
  Menu,
  X,
} from 'lucide-react';
import { SidebarActionItem, SidebarNavItem } from '../design-system';

interface AppLayoutProps {
  children: React.ReactNode;
  onLogout: () => void;
}

export function AppLayout({ children, onLogout }: AppLayoutProps) {
  const [isMobileNavOpen, setIsMobileNavOpen] = useState(false);
  const navItems = [
    { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard, path: '/' },
    { id: 'bank', label: 'Extracto Bancario', icon: Landmark, path: '/bank' },
    { id: 'buckets', label: 'Espacios', icon: PiggyBank, path: '/spaces' },
    { id: 'entities', label: 'Entidades', icon: Users, path: '/entities' },
    { id: 'journal', label: 'Libro Diario', icon: BookOpen, path: '/journal' },
    { id: 'coa', label: 'Plan Contable', icon: ListTree, path: '/coa' },
  ];

  const sidebarContent = (onNavigate?: () => void) => (
    <>
      <div className="min-h-0 flex-1 overflow-y-auto py-6">
        {navItems.map((item) => (
          <SidebarNavItem key={item.id} to={item.path} icon={item.icon} onClick={onNavigate}>
            {item.label}
          </SidebarNavItem>
        ))}
      </div>

      <div className="shrink-0 border-t border-border p-4">
        <SidebarActionItem
          icon={LogOut}
          onClick={() => {
            onNavigate?.();
            onLogout();
          }}
        >
          Cerrar Sesión
        </SidebarActionItem>
      </div>

      <div className="shrink-0 border-t border-border p-6 text-[10px] font-mono text-text-secondary uppercase tracking-widest opacity-50">
        © 2026 CashClarity - v1.0.0
      </div>
    </>
  );

  return (
    <div className="flex h-screen overflow-hidden bg-background text-text-primary font-sans">
      <div className="flex min-w-0 flex-1 flex-col">
        <header className="relative z-50 shrink-0 bg-surface border-b border-border p-4 flex items-center justify-between">
          <div className="flex items-center gap-3">
            <ShieldCheck className="w-6 h-6 text-primary-orange" />
            <h1 className="text-lg font-bold tracking-tight uppercase font-mono">CashClarity</h1>
          </div>
          <button
            type="button"
            className="md:hidden inline-flex h-10 w-10 items-center justify-center rounded-sm border border-border text-text-secondary hover:bg-surface-elevated hover:text-text-primary"
            aria-label={isMobileNavOpen ? 'Cerrar menú' : 'Abrir menú'}
            aria-expanded={isMobileNavOpen}
            onClick={() => setIsMobileNavOpen((open) => !open)}
          >
            {isMobileNavOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
          </button>
        </header>

      <div className="relative flex min-h-0 flex-1 overflow-hidden">
        <nav className="hidden w-64 shrink-0 bg-surface border-r border-border md:flex md:flex-col">
          {sidebarContent()}
        </nav>

        {isMobileNavOpen && (
          <div className="fixed inset-0 z-40 md:hidden">
            <button
              type="button"
              className="absolute inset-0 bg-background/70"
              aria-label="Cerrar panel"
              onClick={() => setIsMobileNavOpen(false)}
            />
            <nav className="relative z-10 flex h-full w-[min(20rem,85vw)] flex-col bg-surface border-r border-border pt-[73px] shadow-2xl">
              {sidebarContent(() => setIsMobileNavOpen(false))}
            </nav>
          </div>
        )}

        <main className="min-w-0 flex-1 overflow-y-auto bg-background p-4 sm:p-6 lg:p-8">
          <div className="mx-auto w-full max-w-7xl responsive-content">
            {children}
          </div>
        </main>
      </div>
      </div>
    </div>
  );
}
