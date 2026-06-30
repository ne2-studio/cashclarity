import { create } from 'zustand';
import { Account, JournalEntry, BankMovement, JournalLine } from '../types';
import { api } from '../api';

interface FinanceStore {
  accounts: Account[];
  journalEntries: JournalEntry[];
  bankMovements: BankMovement[];
  isLoading: boolean;
  error: string | null;
  
  // Actions
  fetchData: () => Promise<void>;
  
  addAccount: (account: Omit<Account, 'id'>) => Promise<Account>;
  updateAccount: (id: string, updates: Partial<Account>) => Promise<void>;
  deleteAccount: (id: string) => Promise<void>;
  
  addJournalEntry: (entry: Omit<JournalEntry, 'id'>) => Promise<JournalEntry>;
  updateJournalEntry: (id: string, updates: Partial<JournalEntry>) => Promise<void>;
  deleteJournalEntry: (id: string) => Promise<void>;
  updateJournalLine: (entryId: string, lineId: string, updates: Partial<JournalLine>) => Promise<void>;
  
  addBankMovement: (movement: Omit<BankMovement, 'id' | 'isIdentified'>) => Promise<BankMovement>;
  updateBankMovement: (id: string, updates: Partial<BankMovement>) => Promise<void>;
  deleteBankMovement: (id: string) => Promise<void>;
}

export const useFinanceStore = create<FinanceStore>((set, get) => ({
  accounts: [],
  journalEntries: [],
  bankMovements: [],
  isLoading: false,
  error: null,

  fetchData: async () => {
    set({ isLoading: true, error: null });
    try {
      const [accounts, journalEntries, bankMovements] = await Promise.all([
        api.accounts.getAll(),
        api.journalEntries.getAll(),
        api.bankMovements.getAll(),
      ]);

      set({ accounts, journalEntries, bankMovements, isLoading: false });
    } catch (error) {
      set({ error: (error as Error).message, isLoading: false });
    }
  },

  addAccount: async (account) => {
    const newAccount = await api.accounts.create(account);
    set((state) => ({ accounts: [...state.accounts, newAccount] }));
    return newAccount;
  },

  updateAccount: async (id, updates) => {
    await api.accounts.update(id, updates);
    set((state) => ({
      accounts: state.accounts.map((a) => (a.id === id ? new Account({ ...a, ...updates }) : a)),
    }));
  },

  deleteAccount: async (id) => {
    await api.accounts.delete(id);
    set((state) => ({ accounts: state.accounts.filter((a) => a.id !== id) }));
  },

  addJournalEntry: async (entry) => {
    const newEntry = await api.journalEntries.create(entry);
    set((state) => ({ journalEntries: [newEntry, ...state.journalEntries] }));
    return newEntry;
  },

  updateJournalEntry: async (id, updates) => {
    // Ensure that if lines are updated, they are full JournalLine objects
    const entryUpdates = { ...updates };
    if (entryUpdates.lines) {
      entryUpdates.lines = entryUpdates.lines.map(l => l instanceof JournalLine ? l : new JournalLine(l));
    }
    await api.journalEntries.update(id, entryUpdates);
    set((state) => ({
      journalEntries: state.journalEntries.map((e) => (e.id === id ? new JournalEntry({ ...e, ...entryUpdates }) : e)),
    }));
  },

  deleteJournalEntry: async (id) => {
    await api.journalEntries.delete(id);
    set((state) => ({
      journalEntries: state.journalEntries.filter((e) => e.id !== id),
    }));
  },

  updateJournalLine: async (entryId, lineId, updates) => {
    const entry = get().journalEntries.find(e => e.id === entryId);
    if (!entry) return;
    
    const newLines = entry.lines.map(l => {
      if (l.id === lineId) {
        return new JournalLine({ ...l, ...updates });
      }
      return l;
    });
    
    await api.journalEntries.update(entryId, { lines: newLines });

    set((state) => ({
      journalEntries: state.journalEntries.map((e) => {
        if (e.id !== entryId) return e;
        return new JournalEntry({
          ...e,
          lines: newLines,
        });
      }),
    }));
  },

  addBankMovement: async (movement) => {
    const newMovement = await api.bankMovements.create(movement);
    set((state) => ({ bankMovements: [newMovement, ...state.bankMovements] }));
    return newMovement;
  },

  updateBankMovement: async (id, updates) => {
    await api.bankMovements.update(id, updates);
    set((state) => ({
      bankMovements: state.bankMovements.map((m) => (m.id === id ? new BankMovement({ ...m, ...updates }) : m)),
    }));
  },

  deleteBankMovement: async (id) => {
    await api.bankMovements.delete(id);
    set((state) => ({
      bankMovements: state.bankMovements.filter((m) => m.id !== id),
    }));
  },
}));
