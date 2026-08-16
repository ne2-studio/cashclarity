import type { Decorator } from '@storybook/react-vite';
import { useFinanceStore } from '../store/useFinanceStore';
import { Account, BankMovement, JournalEntry, JournalLine } from '../types';

export const fixtureAccounts = [
  new Account({ id: 'main', code: '0001', name: 'Cuenta Principal', type: 'main', isSystem: true, active: true }),
  new Account({ id: 'iva', code: '4750', name: 'IVA trimestral', type: 'space', active: true }),
  new Account({ id: 'tax', code: '4751', name: 'Impuestos', type: 'space', active: true }),
  new Account({ id: 'client', code: '4300', name: 'Cliente Norte', type: 'entity', active: true }),
  new Account({ id: 'uncategorized', code: '9999', name: 'Sin categorizar', type: 'uncategorized', isSystem: true, active: true }),
];

export const fixtureEntries = [
  new JournalEntry({
    id: 'entry-1',
    date: '2026-08-16T00:00:00.000Z',
    description: 'Factura cobrada',
    lines: [
      new JournalLine({ id: 'line-1', accountId: 'main', debit: 1200, credit: 0 }),
      new JournalLine({ id: 'line-2', accountId: 'iva', debit: 0, credit: 252 }),
      new JournalLine({ id: 'line-3', accountId: 'client', debit: 0, credit: 948 }),
    ],
  }),
];

export const fixtureMovements = [
  new BankMovement({
    id: 'movement-1',
    date: '2026-08-16T00:00:00.000Z',
    description: 'Transferencia cliente norte',
    amount: 1200,
    isIdentified: true,
    entityId: 'client',
    journalEntryId: 'entry-1',
  }),
];

export const withFinanceFixtures: Decorator = (Story) => {
  useFinanceStore.setState({
    accounts: fixtureAccounts,
    journalEntries: fixtureEntries,
    bankMovements: fixtureMovements,
    isLoading: false,
    error: null,
  });

  return <Story />;
};
