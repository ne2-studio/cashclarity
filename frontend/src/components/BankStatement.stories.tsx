import type { Meta, StoryObj } from '@storybook/react-vite';
import { BankStatement } from './BankStatement';
import { fixtureAccounts, fixtureEntries, fixtureMovements } from '../storybook/financeFixtures';

const meta = {
  title: 'Screens/BankStatement',
  component: BankStatement,
  args: {
    accounts: fixtureAccounts,
    journalEntries: fixtureEntries,
    bankMovements: fixtureMovements,
    onAddBankMovement: async (movement) => ({ id: 'new-movement', isIdentified: false, ...movement }),
    onUpdateBankMovement: async () => undefined,
    onDeleteBankMovement: async () => undefined,
    onAddJournalEntry: async (entry) => ({ id: 'new-entry', ...entry }),
    onUpdateJournalEntry: async () => undefined,
  },
} satisfies Meta<typeof BankStatement>;

export default meta;
type Story = StoryObj<typeof meta>;

export const WithMovements: Story = {};
