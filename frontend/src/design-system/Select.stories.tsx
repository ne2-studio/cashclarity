import type { Meta, StoryObj } from '@storybook/react-vite';
import { Select } from './Select';

const meta = {
  title: 'Design System/Select',
  component: Select,
} satisfies Meta<typeof Select>;

export default meta;
type Story = StoryObj<typeof meta>;

export const AccountType: Story = {
  render: () => (
    <Select defaultValue="space">
      <option value="space">ESPACIO</option>
      <option value="entity">ENTIDAD</option>
    </Select>
  ),
};
