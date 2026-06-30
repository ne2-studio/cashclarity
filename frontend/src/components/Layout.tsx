import React from 'react';
import { NavLink } from 'react-router-dom';
import { 
  LayoutDashboard, 
  PiggyBank, 
  Users, 
  ShieldCheck, 
  BookOpen,
  ListTree,
  Landmark,
  LogOut
} from 'lucide-react';

interface LayoutProps {
  children: React.ReactNode;
  onLogout: () => void;
}

export function Layout({ children, onLogout }: LayoutProps) {
  const navItems = [
    { id: 'dashboard', label: 'Dashboard', icon: LayoutDashboard, path: '/' },
    { id: 'bank', label: 'Extracto Bancario', icon: Landmark, path: '/bank' },
    { id: 'buckets', label: 'Espacios', icon: PiggyBank, path: '/spaces' },
    { id: 'entities', label: 'Entidades', icon: Users, path: '/entities' },
    { id: 'journal', label: 'Libro Diario', icon: BookOpen, path: '/journal' },
    { id: 'coa', label: 'Plan Contable', icon: ListTree, path: '/coa' },
  ];

  return (
    <div className="min-h-screen bg-background text-text-primary font-sans flex flex-col">
      {/* Header */}
      <header className="bg-surface border-b border-border p-4 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <ShieldCheck className="w-6 h-6 text-primary-orange" />
          <h1 className="text-lg font-bold tracking-tight uppercase font-mono">CashClarity</h1>
        </div>
        <div className="text-[10px] text-text-secondary font-mono tracking-widest uppercase">
          v1.0.0
        </div>
      </header>

      <div className="flex flex-1 overflow-hidden">
        {/* Sidebar */}
        <nav className="w-64 bg-surface border-r border-border flex flex-col">
          <div className="flex-1 py-6">
            {navItems.map((item) => (
              <NavLink
                key={item.id}
                to={item.path}
                className={({ isActive }) => `w-full flex items-center gap-3 px-6 py-2.5 text-sm font-medium transition-all duration-200 border-r-2 ${
                  isActive
                    ? 'bg-surface-elevated text-text-primary border-primary-orange'
                    : 'text-text-secondary border-transparent hover:text-text-primary hover:bg-surface-elevated/50'
                }`}
              >
                {({ isActive }) => (
                  <>
                    <item.icon className={`w-4 h-4 ${isActive ? 'text-primary-orange' : ''}`} />
                    {item.label}
                  </>
                )}
              </NavLink>
            ))}
          </div>
          
          <div className="p-4 border-t border-border">
            <button 
              onClick={onLogout}
              className="w-full flex items-center gap-3 px-6 py-2.5 text-sm font-medium text-text-secondary hover:text-primary-orange hover:bg-surface-elevated/50 transition-all duration-200 rounded-sm"
            >
              <LogOut className="w-4 h-4" />
              Cerrar Sesión
            </button>
          </div>

          <div className="p-6 border-t border-border text-[10px] font-mono text-text-secondary uppercase tracking-widest opacity-50">
            © 2026 CashClarity
          </div>
        </nav>

        {/* Main Content */}
        <main className="flex-1 overflow-y-auto p-8 bg-background">
          <div className="max-w-7xl mx-auto">
            {children}
          </div>
        </main>
      </div>
    </div>
  );
}
