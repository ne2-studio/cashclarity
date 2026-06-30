import React, { useMemo } from 'react';
import { useFinanceStore } from '../store/useFinanceStore';
import { 
  PiggyBank, 
  Wallet,
  ShieldCheck
} from 'lucide-react';

import { Account } from '../types';

interface DashboardProps {
  metrics: {
    realBankBalance: number;
    totalCommitted: number;
    availableCash: number;
    bucketBalances: Record<string, number>;
  };
}

export function Dashboard({ metrics }: DashboardProps) {
  const { accounts } = useFinanceStore();
  const { realBankBalance, totalCommitted, availableCash, bucketBalances } = metrics;
  
  const displayAccounts = useMemo(() => {
    return accounts.filter((a: Account) => a.type === 'main' || a.type === 'space')
      .sort((a: Account, b: Account) => {
        if (a.type === 'main') return -1;
        if (b.type === 'main') return 1;
        return a.code.localeCompare(b.code);
      });
  }, [accounts]);

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const StatCard = ({ title, value, icon: Icon, type = 'neutral', subtext = '' }: { 
    title: string; 
    value: number | string; 
    icon: React.ElementType; 
    type?: 'positive' | 'negative' | 'warning' | 'neutral'; 
    subtext?: string; 
  }) => {
    const colorClass = 
      type === 'positive' ? 'text-primary-green' : 
      type === 'negative' ? 'text-error' : 
      type === 'warning' ? 'text-primary-orange' : 
      'text-text-primary';

    return (
      <div className="financial-card flex flex-col gap-1">
        <div className="flex items-center justify-between mb-2">
          <span className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">{title}</span>
          <Icon className={`w-3.5 h-3.5 ${colorClass} opacity-80`} />
        </div>
        <div className={`text-xl font-bold numeric ${colorClass}`}>
          {typeof value === 'number' ? formatCurrency(value) : value}
        </div>
        {subtext && <div className="text-[10px] text-text-secondary font-mono mt-1 opacity-60">{subtext}</div>}
      </div>
    );
  };

  const SectionHeader = ({ title }: { title: string }) => (
    <div className="flex items-center gap-4 mb-6">
      <h2 className="text-[10px] font-mono font-bold uppercase tracking-[0.2em] text-text-secondary whitespace-nowrap">
        {title}
      </h2>
      <div className="h-px w-full bg-border" />
    </div>
  );

  return (
    <div className="flex flex-col gap-12">
      {/* Cash Visibility Section */}
      <section>
        <SectionHeader title="Visibilidad de Caja // Tesorería Real" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <StatCard 
            title="Saldo Bancario Real" 
            value={realBankBalance} 
            icon={Wallet} 
            type="positive" 
            subtext="Total consolidado en bancos"
          />
          <StatCard 
            title="Saldo Comprometido" 
            value={totalCommitted} 
            icon={PiggyBank} 
            type="warning"
            subtext="Asignado a espacios de reserva"
          />
          <StatCard 
            title="Saldo Disponible" 
            value={availableCash} 
            icon={ShieldCheck} 
            type={availableCash >= 0 ? 'positive' : 'negative'}
            subtext="Caja libre para operaciones"
          />
        </div>
      </section>

      {/* Bucket Summary Section */}
      <section>
        <SectionHeader title="Espacios // Reservas y Provisiones" />
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          <div>
            <div className="bg-surface border border-border rounded-sm overflow-hidden">
              <table className="w-full">
                <thead>
                  <tr>
                    <th className="p-3 text-[9px]">Espacio</th>
                    <th className="p-3 text-[9px] text-right">Saldo Actual</th>
                  </tr>
                </thead>
                <tbody className="text-xs font-mono">
                  {displayAccounts.map((b: Account) => {
                    const balance = bucketBalances[b.id] || 0;
                    
                    return (
                      <tr key={b.id} className="hover:bg-surface-elevated/50 transition-colors">
                        <td className="p-3">
                          <div className="flex items-center gap-2">
                            {b.type === 'main' ? (
                              <ShieldCheck className="w-3 h-3 text-primary-green" />
                            ) : (
                              <PiggyBank className="w-3 h-3 text-text-secondary" />
                            )}
                            <span className={`font-medium ${b.type === 'main' ? 'text-primary-green' : ''}`}>
                              {b.name}
                            </span>
                          </div>
                        </td>
                        <td className={`p-3 numeric font-bold text-right ${balance >= 0 ? 'text-primary-green' : 'text-error'}`}>
                          {formatCurrency(balance)}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>

          <div className="flex flex-col gap-4">
            <div className="financial-card bg-surface-elevated/20">
              <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-4">Análisis de Liquidez</h3>
              <div className="flex flex-col gap-6">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-text-secondary">Ratio de Reserva</span>
                  <span className="text-sm font-bold numeric">
                    {new Intl.NumberFormat('es-ES', { style: 'percent' }).format(totalCommitted / realBankBalance)}
                  </span>
                </div>
                <div className="w-full h-2 bg-background border border-border rounded-full overflow-hidden flex">
                  <div 
                    className="h-full bg-primary-orange" 
                    style={{ width: `${(totalCommitted / realBankBalance) * 100}%` }} 
                  />
                  <div 
                    className="h-full bg-primary-green" 
                    style={{ width: `${(availableCash / realBankBalance) * 100}%` }} 
                  />
                </div>
                <div className="flex gap-4 text-[9px] font-mono uppercase">
                  <div className="flex items-center gap-1.5">
                    <div className="w-2 h-2 bg-primary-orange rounded-full" />
                    <span>Comprometido</span>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <div className="w-2 h-2 bg-primary-green rounded-full" />
                    <span>Disponible</span>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}
