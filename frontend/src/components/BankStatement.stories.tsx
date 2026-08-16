import type { Meta, StoryObj } from '@storybook/react-vite';
import { BankStatement } from './BankStatement';
import { withFinanceFixtures } from '../storybook/financeFixtures';

const meta = {
  title: 'Screens/BankStatement',
  component: BankStatement,
  decorators: [withFinanceFixtures],
} satisfies Meta<typeof BankStatement>;

export default meta;
type Story = StoryObj<typeof meta>;

export const WithMovements: Story = {};
