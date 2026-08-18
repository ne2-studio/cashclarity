import type { Meta, StoryObj } from '@storybook/react-vite';
import { MemoryRouter } from 'react-router-dom';
import { LayoutDashboard } from 'lucide-react';
import { SidebarNavItem } from './SidebarNavItem';

const meta = {
  title: 'Design System/SidebarNavItem',
  component: SidebarNavItem,
  decorators: [
    (Story) => (
      <MemoryRouter initialEntries={['/']}>
        <div className="w-64 bg-surface py-6">
          <Story />
        </div>
      </MemoryRouter>
    ),
  ],
} satisfies Meta<typeof SidebarNavItem>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Active: Story = {
  args: {
    to: '/',
    icon: LayoutDashboard,
    children: 'Dashboard',
  },
};
