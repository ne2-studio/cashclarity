import type { Meta, StoryObj } from '@storybook/react-vite';
import { Card } from './Card';

const meta = {
  title: 'Design System/Card',
  component: Card,
} satisfies Meta<typeof Card>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  render: () => (
    <Card className="p-5 max-w-sm">
      <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-2">Panel</h3>
      <p className="text-sm text-text-primary">Contenido en superficie del sistema.</p>
    </Card>
  ),
};
