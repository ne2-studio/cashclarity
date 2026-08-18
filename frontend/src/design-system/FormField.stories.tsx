import type { Meta, StoryObj } from '@storybook/react-vite';
import { FormField } from './FormField';
import { Input } from './Input';

const meta = {
  title: 'Design System/FormField',
  component: FormField,
} satisfies Meta<typeof FormField>;

export default meta;
type Story = StoryObj<typeof meta>;

export const WithInput: Story = {
  args: {
    label: 'Código (4 dígitos)',
    children: <Input placeholder="5721" />,
  },
  render: () => (
    <FormField label="Código (4 dígitos)">
      <Input placeholder="5721" />
    </FormField>
  ),
};
