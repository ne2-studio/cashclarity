import { useState, useMemo } from 'react';
import { useFinanceStore } from '../store/useFinanceStore';
import { 
  Search, 
  History
} from 'lucide-react';
import { JournalEntry, JournalLine, Account } from '../types';

export function Journal() {
  const { journalEntries, accounts } = useFinanceStore();
  const [searchTerm, setSearchTerm] = useState('');

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const flattenedLines = useMemo(() => {
    const lines: (JournalLine & { entryId: string; date: string; description: string })[] = [];
    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach((line: JournalLine) => {
        lines.push({
          ...line,
          entryId: entry.id,
          date: entry.date,
          description: entry.description
        });
      });
    });
    
    const search = searchTerm.toLowerCase();
    return lines.filter(l => 
      l.description.toLowerCase().includes(search) || 
      accounts.find((a: Account) => a.id === l.accountId)?.name.toLowerCase().includes(search) ||
      accounts.find((a: Account) => a.id === l.accountId)?.code.includes(search)
    ).sort((a, b) => new Date(b.date).getTime() - new Date(a.date).getTime());
  }, [journalEntries, accounts, searchTerm]);

  return (
    <div className="flex flex-col gap-8">
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-1">
          <h2 className="text-lg font-medium tracking-tight text-text-primary flex items-center gap-2">
            <History className="w-4 h-4 text-text-secondary" />
            Libro Diario // Registro Contable
          </h2>
          <p className="text-xs text-text-secondary">Asientos y apuntes contables</p>
        </div>
      </div>

      <div className="flex items-center gap-4 bg-surface border border-border p-4 rounded-sm">
        <div className="flex-1 relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-secondary" />
          <input 
            type="text" 
            placeholder="Buscar por concepto o cuenta..." 
            value={searchTerm}
            onChange={e => setSearchTerm(e.target.value)}
            className="w-full bg-background border border-border pl-10 pr-4 py-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
          />
        </div>
      </div>

      <div className="bg-surface border border-border rounded-sm overflow-hidden">
        <table className="w-full text-left">
          <thead>
            <tr className="bg-surface-elevated/50 border-b border-border">
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary">Fecha</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary">Concepto</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary">Cuenta</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary text-right">Debe</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary text-right">Haber</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/30">
            {flattenedLines.map((line, idx: number) => {
              const account = accounts.find((a: Account) => a.id === line.accountId);

              return (
                <tr key={`${line.entryId}-${line.id || idx}`} className="hover:bg-surface-elevated/20 transition-colors group">
                  <td className="p-4 text-xs font-mono text-text-secondary">{line.date.split('T')[0]}</td>
                  <td className="p-4">
                    <span className="text-xs font-medium">{line.description}</span>
                  </td>
                  <td className="p-4">
                    <div className="flex flex-col">
                      <span className="text-xs font-bold">{account?.code}</span>
                      <span className="text-[10px] text-text-secondary uppercase">{account?.name}</span>
                    </div>
                  </td>
                  <td className="p-4 text-right">
                    <span className="text-xs font-mono">{line.debit > 0 ? formatCurrency(line.debit) : '-'}</span>
                  </td>
                  <td className="p-4 text-right">
                    <span className="text-xs font-mono">{line.credit > 0 ? formatCurrency(line.credit) : '-'}</span>
                  </td>
                </tr>
              );
            })}
            {flattenedLines.length === 0 && (
              <tr key="empty-journal">
                <td colSpan={5} className="p-12 text-center text-text-secondary italic text-xs">
                  No se han encontrado asientos contables.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
