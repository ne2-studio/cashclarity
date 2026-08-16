import type { Meta, StoryObj } from '@storybook/react-vite';
import { ChartOfAccounts } from './ChartOfAccounts';
import { withFinanceFixtures } from '../storybook/financeFixtures';

const meta = {
  title: 'Screens/ChartOfAccounts',
  component: ChartOfAccounts,
  decorators: [withFinanceFixtures],
} satisfies Meta<typeof ChartOfAccounts>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Populated: Story = {};
