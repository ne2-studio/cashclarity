import { PiggyBank, ShieldCheck } from 'lucide-react';
import { Card, SectionHeader } from '../design-system';
import type { Account } from '../types';

interface BucketSummarySectionProps {
  availableCash: number;
  bucketBalances: Record<string, number>;
  displayAccounts: Account[];
  totalCommitted: number;
  realBankBalance: number;
  formatCurrency: (value: number) => string;
}

export function calculateLiquidityRatios(realBankBalance: number, totalCommitted: number, availableCash: number) {
  const committedRatio = realBankBalance > 0 ? totalCommitted / realBankBalance : 0;
  const availableRatio = realBankBalance > 0 ? availableCash / realBankBalance : 0;
  const toPercentWidth = (value: number) => `${Math.min(Math.max(value, 0), 1) * 100}%`;

  return {
    committedRatio,
    availableRatio,
    committedWidth: toPercentWidth(committedRatio),
    availableWidth: toPercentWidth(availableRatio),
  };
}

export function BucketSummarySection({
  availableCash,
  bucketBalances,
  displayAccounts,
  totalCommitted,
  realBankBalance,
  formatCurrency,
}: BucketSummarySectionProps) {
  const { committedRatio, committedWidth, availableWidth } = calculateLiquidityRatios(
    realBankBalance,
    totalCommitted,
    availableCash,
  );

  return (
    <section>
      <SectionHeader title="Espacios // Reservas y Provisiones" />
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
        <div>
          <Card className="overflow-x-auto">
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
          </Card>
        </div>

        <div className="flex flex-col gap-4">
          <div className="financial-card bg-surface-elevated/20">
            <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-4">Análisis de Liquidez</h3>
            <div className="flex flex-col gap-6">
              <div className="flex items-center justify-between">
                <span className="text-sm text-text-secondary">Ratio de Reserva</span>
                <span className="text-sm font-bold numeric">
                  {new Intl.NumberFormat('es-ES', { style: 'percent' }).format(committedRatio)}
                </span>
              </div>
              <div className="w-full h-2 bg-background border border-border rounded-full overflow-hidden flex">
                <div
                  className="h-full bg-primary-orange"
                  style={{ width: committedWidth }}
                />
                <div
                  className="h-full bg-primary-green"
                  style={{ width: availableWidth }}
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
  );
}
