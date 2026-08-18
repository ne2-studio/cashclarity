import type { Meta, StoryObj } from '@storybook/react-vite';
import { Wallet } from 'lucide-react';
import { StatCard } from './StatCard';

const meta = {
  title: 'Design System/StatCard',
  component: StatCard,
} satisfies Meta<typeof StatCard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    title: 'Saldo Bancario Real',
    value: '1.200,00 EUR',
    icon: Wallet,
    tone: 'positive',
    subtext: 'Total consolidado en bancos',
  },
};
