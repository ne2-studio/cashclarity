import type { Meta, StoryObj } from '@storybook/react-vite';
import { Plus } from 'lucide-react';
import { Button } from './Button';

const meta = {
  title: 'Design System/Button',
  component: Button,
} satisfies Meta<typeof Button>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Variants: Story = {
  render: () => (
    <div className="flex items-center gap-3">
      <Button icon={<Plus className="w-3.5 h-3.5" />}>Principal</Button>
      <Button variant="secondary">Secundario</Button>
      <Button variant="ghost">Fantasma</Button>
      <Button variant="link">+ Añadir línea</Button>
    </div>
  ),
};
