import { describe, expect, it } from 'vitest';
import { BankMovement, JournalEntry, JournalLine } from '../types';
import {
  appendReservationLines,
  createEntryFromMovement,
  getJournalTotals,
  identifyEntryLines,
  payEntryFromSpaceLines,
  toEditableJournalEntry,
  validateJournalEntry,
  validateReservations,
} from './journalEntryLogic';

const movement = new BankMovement({
  id: 'm1',
  date: '2026-02-01',
  description: 'Ingreso',
  amount: 100,
  isIdentified: false,
});

describe('journal entry logic', () => {
  it('creates a balanced entry from an income movement', () => {
    const entry = createEntryFromMovement(movement, 'main', 'uncat', idSequence('l1', 'l2'));

    expect(entry.lines).toEqual([
      new JournalLine({ id: 'l1', accountId: 'main', debit: 100, credit: 0 }),
      new JournalLine({ id: 'l2', accountId: 'uncat', debit: 0, credit: 100 }),
    ]);
    expect(getJournalTotals(entry.lines)).toEqual({ debit: 100, credit: 100 });
  });

  it('creates a balanced entry from an expense movement', () => {
    const entry = createEntryFromMovement(
      new BankMovement({ ...movement, amount: -25 }),
      'main',
      'uncat',
      idSequence('l1', 'l2'),
    );

    expect(entry.lines).toEqual([
      new JournalLine({ id: 'l1', accountId: 'main', debit: 0, credit: 25 }),
      new JournalLine({ id: 'l2', accountId: 'uncat', debit: 25, credit: 0 }),
    ]);
  });

  it('validates missing accounts and unbalanced entries', () => {
    expect(
      validateJournalEntry({ description: 'x', date: '2026-01-01', lines: [line('l1', '', 1, 1)] }),
    ).toBe('Todas las líneas deben tener una cuenta seleccionada');
    expect(
      validateJournalEntry({ description: 'x', date: '2026-01-01', lines: [line('l1', 'a1', 2, 1)] }),
    ).toBe('El asiento no está cuadrado');
  });

  it('clones editable entries instead of reusing line references', () => {
    const original = new JournalEntry({
      id: 'e1',
      date: '2026-01-01',
      description: 'x',
      lines: [line('l1', 'a1', 1, 1)],
    });

    const editable = toEditableJournalEntry(original);
    editable.lines[0].accountId = 'changed';

    expect(original.lines[0].accountId).toBe('a1');
  });

  it('moves uncategorized and main lines to selected accounts', () => {
    const lines = [line('l1', 'main', 10, 0), line('l2', 'uncat', 0, 10)];

    expect(identifyEntryLines(lines, 'uncat', 'entity').map((item) => item.accountId)).toEqual(['main', 'entity']);
    expect(payEntryFromSpaceLines(lines, 'main', 'space').map((item) => item.accountId)).toEqual(['space', 'uncat']);
  });

  it('validates and appends reservation debit-credit pairs', () => {
    const reservations = [{ spaceId: 'space', amount: 30 }];

    expect(validateReservations(reservations, 100, true)).toBeNull();
    expect(validateReservations(reservations, 20, true)).toBe(
      'La cantidad total reservada no puede superar el importe del movimiento',
    );
    expect(validateReservations([{ spaceId: '', amount: 30 }], 100, true)).toBe(
      'Todas las reservas deben tener un espacio seleccionado y una cantidad mayor que cero',
    );

    expect(appendReservationLines([line('base', 'main', 100, 0)], reservations, 'main', idSequence('r1', 'r2'))).toEqual([
      line('base', 'main', 100, 0),
      line('r1', 'space', 30, 0),
      line('r2', 'main', 0, 30),
    ]);
  });
});

function line(id: string, accountId: string, debit: number, credit: number) {
  return new JournalLine({ id, accountId, debit, credit });
}

function idSequence(...ids: string[]) {
  let index = 0;
  return () => ids[index++];
}
