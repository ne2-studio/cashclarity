import { describe, expect, it } from 'vitest';
import { Account, JournalEntry, JournalLine } from '../types';
import {
  buildAccountStats,
  filterAndSortAccounts,
  getCashDisplayAccounts,
  getEntityAccounts,
  isValidAccountCode,
} from './accountViews';

const accounts = [
  account('space2', '5722', 'IVA', 'space'),
  account('entity', '4300', 'Cliente', 'entity'),
  account('main', '5720', 'Banco', 'main'),
  account('space1', '5721', 'Nominas', 'space'),
];

describe('account views', () => {
  it('validates four digit account codes', () => {
    expect(isValidAccountCode('5721')).toBe(true);
    expect(isValidAccountCode('572')).toBe(false);
    expect(isValidAccountCode('57A1')).toBe(false);
  });

  it('orders cash display accounts with main first', () => {
    expect(getCashDisplayAccounts(accounts).map((item) => item.id)).toEqual(['main', 'space1', 'space2']);
  });

  it('filters entities and searchable accounts', () => {
    expect(getEntityAccounts(accounts).map((item) => item.id)).toEqual(['entity']);
    expect(filterAndSortAccounts(accounts, '572').map((item) => item.id)).toEqual(['main', 'space1', 'space2']);
  });

  it('builds debit and credit totals per account', () => {
    const stats = buildAccountStats(accounts, [
      new JournalEntry({
        id: 'e1',
        date: '2026-01-01',
        description: 'x',
        lines: [
          new JournalLine({ id: 'l1', accountId: 'main', debit: 10, credit: 0 }),
          new JournalLine({ id: 'l2', accountId: 'entity', debit: 0, credit: 10 }),
        ],
      }),
    ]);

    expect(stats.main).toEqual({ debit: 10, credit: 0 });
    expect(stats.entity).toEqual({ debit: 0, credit: 10 });
    expect(stats.space1).toEqual({ debit: 0, credit: 0 });
  });
});

function account(id: string, code: string, name: string, type: Account['type']) {
  return new Account({ id, code, name, type, active: true });
}
