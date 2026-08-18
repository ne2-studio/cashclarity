import type { Meta, StoryObj } from '@storybook/react-vite';
import { Input } from './Input';

const meta = {
  title: 'Design System/Input',
  component: Input,
} satisfies Meta<typeof Input>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    placeholder: 'Importe',
  },
};
