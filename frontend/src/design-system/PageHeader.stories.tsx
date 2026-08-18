import type { Meta, StoryObj } from '@storybook/react-vite';
import { PiggyBank } from 'lucide-react';
import { Button } from './Button';
import { PageHeader } from './PageHeader';

const meta = {
  title: 'Design System/PageHeader',
  component: PageHeader,
} satisfies Meta<typeof PageHeader>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    icon: <PiggyBank className="w-4 h-4 text-primary-orange" />,
    title: 'Espacios de Reserva',
    subtitle: 'Gestión de fondos comprometidos y provisiones',
    actions: <Button>Nuevo Espacio</Button>,
  },
};
