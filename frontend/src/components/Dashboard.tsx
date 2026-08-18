import { 
  PiggyBank, 
  Wallet,
  ShieldCheck
} from 'lucide-react';

import type { Account } from '../types';
import { useDashboardViewModel } from '../hooks/accountViews';
import { SectionHeader, StatCard } from '../design-system';
import { BucketSummarySection } from './BucketSummarySection';

interface DashboardProps {
  accounts: Account[];
  metrics: {
    realBankBalance: number;
    totalCommitted: number;
    availableCash: number;
    bucketBalances: Record<string, number>;
  };
}

export function Dashboard({ accounts, metrics }: DashboardProps) {
  const { realBankBalance, totalCommitted, availableCash, bucketBalances } = metrics;
  const { displayAccounts } = useDashboardViewModel(accounts);

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  return (
    <div className="flex flex-col gap-12">
      {/* Cash Visibility Section */}
      <section>
        <SectionHeader title="Visibilidad de Caja // Tesorería Real" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <StatCard 
            title="Saldo Bancario Real" 
            value={formatCurrency(realBankBalance)} 
            icon={Wallet} 
            tone="positive"
            subtext="Total consolidado en bancos"
          />
          <StatCard 
            title="Saldo Comprometido" 
            value={formatCurrency(totalCommitted)}
            icon={PiggyBank} 
            tone="warning"
            subtext="Asignado a espacios de reserva"
          />
          <StatCard 
            title="Saldo Disponible" 
            value={formatCurrency(availableCash)}
            icon={ShieldCheck} 
            tone={availableCash >= 0 ? 'positive' : 'negative'}
            subtext="Caja libre para operaciones"
          />
        </div>
      </section>

      <BucketSummarySection
        availableCash={availableCash}
        bucketBalances={bucketBalances}
        displayAccounts={displayAccounts}
        totalCommitted={totalCommitted}
        realBankBalance={realBankBalance}
        formatCurrency={formatCurrency}
      />
    </div>
  );
}
