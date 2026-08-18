import type { ElementType, ReactNode } from 'react';
import { cx } from './utils';

type StatTone = 'positive' | 'negative' | 'warning' | 'neutral';

interface StatCardProps {
  title: string;
  value: ReactNode;
  icon?: ElementType<{ className?: string }>;
  tone?: StatTone;
  subtext?: string;
}

const toneClasses: Record<StatTone, string> = {
  positive: 'text-primary-green',
  negative: 'text-error',
  warning: 'text-primary-orange',
  neutral: 'text-text-primary',
};

export function StatCard({ title, value, icon: Icon, tone = 'neutral', subtext }: StatCardProps) {
  const toneClass = toneClasses[tone];

  return (
    <div className="financial-card flex flex-col gap-1">
      <div className="flex items-center justify-between mb-2">
        <span className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">{title}</span>
        {Icon && <Icon className={cx('w-3.5 h-3.5 opacity-80', toneClass)} />}
      </div>
      <div className={cx('text-xl font-bold numeric', toneClass)}>{value}</div>
      {subtext && <div className="text-[10px] text-text-secondary font-mono mt-1 opacity-60">{subtext}</div>}
    </div>
  );
}
