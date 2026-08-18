import { JournalLine, type BankMovement, type JournalEntry } from '../types';

export interface EditableJournalEntry {
  description: string;
  date: string;
  lines: JournalLine[];
}

export interface ReservationDraft {
  spaceId: string;
  amount: number;
}

export function createEntryFromMovement(
  movement: BankMovement,
  mainAccountId: string,
  uncategorizedAccountId: string,
  createId: () => string = () => crypto.randomUUID(),
): Omit<JournalEntry, 'id'> {
  const isIncome = movement.amount > 0;
  const absAmount = Math.abs(movement.amount);

  return {
    description: movement.description,
    date: movement.date,
    lines: [
      new JournalLine({
        id: createId(),
        accountId: mainAccountId,
        debit: isIncome ? absAmount : 0,
        credit: isIncome ? 0 : absAmount,
      }),
      new JournalLine({
        id: createId(),
        accountId: uncategorizedAccountId,
        debit: isIncome ? 0 : absAmount,
        credit: isIncome ? absAmount : 0,
      }),
    ],
  };
}

export function toEditableJournalEntry(entry: JournalEntry): EditableJournalEntry {
  return {
    description: entry.description,
    date: entry.date,
    lines: entry.lines.map((line) => new JournalLine({ ...line })),
  };
}

export function cloneJournalLines(lines: JournalLine[]) {
  return lines.map((line) => new JournalLine({ ...line }));
}

export function getJournalTotals(lines: JournalLine[]) {
  return lines.reduce(
    (totals, line) => ({
      debit: totals.debit + line.debit,
      credit: totals.credit + line.credit,
    }),
    { debit: 0, credit: 0 },
  );
}

export function validateJournalEntry(entry: EditableJournalEntry) {
  if (entry.lines.some((line) => !line.accountId)) {
    return 'Todas las líneas deben tener una cuenta seleccionada';
  }

  const totals = getJournalTotals(entry.lines);
  if (Math.abs(totals.debit - totals.credit) > 0.01) {
    return 'El asiento no está cuadrado';
  }

  return null;
}

export function identifyEntryLines(
  lines: JournalLine[],
  uncategorizedAccountId: string | undefined,
  entityId: string,
) {
  return lines.map((line) => {
    if (line.accountId === uncategorizedAccountId) {
      return new JournalLine({ ...line, accountId: entityId });
    }
    return new JournalLine({ ...line });
  });
}

export function payEntryFromSpaceLines(lines: JournalLine[], mainAccountId: string, spaceId: string) {
  return lines.map((line) => {
    if (line.accountId === mainAccountId) {
      return new JournalLine({ ...line, accountId: spaceId });
    }
    return new JournalLine({ ...line });
  });
}

export function validateReservations(
  reservations: ReservationDraft[],
  movementAmount: number,
  hasMainAccount: boolean,
) {
  if (reservations.length === 0) return null;
  if (!hasMainAccount) return 'No se ha encontrado la cuenta principal';

  const totalReserved = reservations.reduce((sum, reservation) => sum + reservation.amount, 0);
  if (totalReserved > movementAmount) {
    return 'La cantidad total reservada no puede superar el importe del movimiento';
  }

  if (reservations.some((reservation) => !reservation.spaceId || reservation.amount <= 0)) {
    return 'Todas las reservas deben tener un espacio seleccionado y una cantidad mayor que cero';
  }

  return null;
}

export function appendReservationLines(
  lines: JournalLine[],
  reservations: ReservationDraft[],
  mainAccountId: string,
  createId: () => string = () => crypto.randomUUID(),
) {
  const nextLines = cloneJournalLines(lines);

  reservations.forEach((reservation) => {
    nextLines.push(
      new JournalLine({
        id: createId(),
        accountId: reservation.spaceId,
        debit: reservation.amount,
        credit: 0,
      }),
    );
    nextLines.push(
      new JournalLine({
        id: createId(),
        accountId: mainAccountId,
        debit: 0,
        credit: reservation.amount,
      }),
    );
  });

  return nextLines;
}
