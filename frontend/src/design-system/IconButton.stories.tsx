import type { Meta, StoryObj } from '@storybook/react-vite';
import { FileEdit, Trash2 } from 'lucide-react';
import { IconButton } from './IconButton';

const meta = {
  title: 'Design System/IconButton',
  component: IconButton,
} satisfies Meta<typeof IconButton>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Variants: Story = {
  args: {
    children: <FileEdit className="w-4 h-4" />,
  },
  render: () => (
    <div className="flex items-center gap-3">
      <IconButton hover="default" aria-label="Editar"><FileEdit className="w-4 h-4" /></IconButton>
      <IconButton aria-label="Eliminar"><Trash2 className="w-4 h-4" /></IconButton>
    </div>
  ),
};
