export type AccountType = 'main' | 'space' | 'entity' | 'uncategorized';

export class Account {
  id: string;
  code: string;
  name: string;
  type: AccountType;
  isSystem?: boolean;
  active: boolean;

  constructor(data: {
    id: string;
    code: string;
    name: string;
    type: AccountType;
    isSystem?: boolean;
    active: boolean;
  }) {
    this.id = data.id;
    this.code = data.code;
    this.name = data.name;
    this.type = data.type;
    this.isSystem = data.isSystem;
    this.active = data.active;
  }
}

export class JournalLine {
  id: string;
  accountId: string;
  debit: number;
  credit: number;

  constructor(data: {
    id: string;
    accountId: string;
    debit: number;
    credit: number;
  }) {
    this.id = data.id;
    this.accountId = data.accountId;
    this.debit = data.debit;
    this.credit = data.credit;
  }
}

export class JournalEntry {
  id: string;
  date: string;
  description: string;
  lines: JournalLine[];

  constructor(data: {
    id: string;
    date: string;
    description: string;
    lines: JournalLine[];
  }) {
    this.id = data.id;
    this.date = data.date;
    this.description = data.description;
    this.lines = data.lines.map(line => line instanceof JournalLine ? line : new JournalLine(line));
  }
}

export class BankMovement {
  id: string;
  date: string;
  description: string;
  amount: number; // Positive for income, negative for expense
  isIdentified: boolean;
  entityId?: string;
  journalEntryId?: string;

  constructor(data: {
    id: string;
    date: string;
    description: string;
    amount: number;
    isIdentified: boolean;
    entityId?: string;
    journalEntryId?: string;
  }) {
    this.id = data.id;
    this.date = data.date;
    this.description = data.description;
    this.amount = data.amount;
    this.isIdentified = data.isIdentified;
    this.entityId = data.entityId;
    this.journalEntryId = data.journalEntryId;
  }
}
