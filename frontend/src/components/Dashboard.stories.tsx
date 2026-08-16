import type { Meta, StoryObj } from '@storybook/react-vite';
import { Dashboard } from './Dashboard';
import { withFinanceFixtures } from '../storybook/financeFixtures';

const meta = {
  title: 'Screens/Dashboard',
  component: Dashboard,
  decorators: [withFinanceFixtures],
  args: {
    metrics: {
      realBankBalance: 1200,
      totalCommitted: 252,
      availableCash: 948,
      bucketBalances: {
        main: 948,
        iva: 252,
        tax: 0,
      },
    },
  },
} satisfies Meta<typeof Dashboard>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {};
