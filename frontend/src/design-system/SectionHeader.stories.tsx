import type { Meta, StoryObj } from '@storybook/react-vite';
import { SectionHeader } from './SectionHeader';

const meta = {
  title: 'Design System/SectionHeader',
  component: SectionHeader,
} satisfies Meta<typeof SectionHeader>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    title: 'Visibilidad de Caja // Tesorería Real',
  },
};
