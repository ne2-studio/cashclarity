import type { Meta, StoryObj } from '@storybook/react-vite';
import { LogOut } from 'lucide-react';
import { SidebarActionItem } from './SidebarActionItem';

const meta = {
  title: 'Design System/SidebarActionItem',
  component: SidebarActionItem,
} satisfies Meta<typeof SidebarActionItem>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    icon: LogOut,
    children: 'Cerrar Sesión',
  },
};
