import { useState } from 'react';
import { 
  History
} from 'lucide-react';
import { JournalEntry, Account } from '../types';
import { useJournalLines } from '../hooks/ledgerViews';
import { Card, PageHeader, SearchField } from '../design-system';

interface JournalProps {
  journalEntries: JournalEntry[];
  accounts: Account[];
}

export function Journal({ journalEntries, accounts }: JournalProps) {
  const [searchTerm, setSearchTerm] = useState('');

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const flattenedLines = useJournalLines(journalEntries, accounts, searchTerm);

  return (
    <div className="flex flex-col gap-8">
      <PageHeader
        icon={<History className="w-4 h-4 text-text-secondary" />}
        title="Libro Diario // Registro Contable"
        subtitle="Asientos y apuntes contables"
      />

      <Card className="flex items-center gap-4 p-4">
        <SearchField
          className="flex-1"
          placeholder="Buscar por concepto o cuenta..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
        />
      </Card>

      <Card className="overflow-x-auto">
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
      </Card>
    </div>
  );
}
