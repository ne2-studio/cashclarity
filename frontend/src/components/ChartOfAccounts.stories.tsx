import type { Meta, StoryObj } from '@storybook/react-vite';
import { ChartOfAccounts } from './ChartOfAccounts';
import { fixtureAccounts, fixtureEntries } from '../storybook/financeFixtures';

const meta = {
  title: 'Screens/ChartOfAccounts',
  component: ChartOfAccounts,
  args: {
    accounts: fixtureAccounts,
    journalEntries: fixtureEntries,
    onAddAccount: async (account) => ({ id: 'new-account', ...account }),
    onDeleteAccount: async () => undefined,
  },
} satisfies Meta<typeof ChartOfAccounts>;

export default meta;
type Story = StoryObj<typeof meta>;

export const Populated: Story = {};
