import { describe, expect, it } from 'vitest';
import { Account, JournalEntry, JournalLine } from '../types';
import {
  filterJournalLines,
  flattenJournalLines,
  getAccountLedgerStats,
  getAccountSummary,
  getAccountTransactions,
} from './ledgerViews';

const accounts = [
  account('main', '5720', 'Banco', 'main'),
  account('entity', '4300', 'Cliente A', 'entity'),
];

const entries = [
  entry('e1', '2026-01-01', 'Factura enero', [
    line('l1', 'main', 100, 0),
    line('l2', 'entity', 0, 100),
  ]),
  entry('e2', '2026-02-01', 'Pago febrero', [
    line('l3', 'entity', 50, 0),
  ]),
];

describe('ledger views', () => {
  it('flattens, filters by account metadata and sorts newest first', () => {
    const lines = filterJournalLines(flattenJournalLines(entries), accounts, 'cliente');

    expect(lines.map((line) => line.id)).toEqual(['l3', 'l2']);
  });

  it('builds account transactions and stats', () => {
    expect(getAccountTransactions(entries, 'entity').map((line) => line.displayAmount)).toEqual([50, -100]);
    expect(getAccountLedgerStats(entries, 'entity')).toMatchObject({
      count: 2,
      totalIn: 50,
      totalOut: 100,
      net: -50,
    });
    expect(getAccountSummary(entries, 'entity')).toEqual({ count: 2, net: -50 });
  });
});

function account(id: string, code: string, name: string, type: Account['type']) {
  return new Account({ id, code, name, type, active: true });
}

function entry(id: string, date: string, description: string, lines: JournalLine[]) {
  return new JournalEntry({ id, date, description, lines });
}

function line(id: string, accountId: string, debit: number, credit: number) {
  return new JournalLine({ id, accountId, debit, credit });
}
