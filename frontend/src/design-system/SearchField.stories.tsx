import type { Meta, StoryObj } from '@storybook/react-vite';
import { SearchField } from './SearchField';

const meta = {
  title: 'Design System/SearchField',
  component: SearchField,
} satisfies Meta<typeof SearchField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    placeholder: 'Buscar por nombre o código...',
  },
};
