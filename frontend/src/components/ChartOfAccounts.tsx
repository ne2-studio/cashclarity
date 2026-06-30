import { useState, useMemo } from 'react';
import { useFinanceStore } from '../store/useFinanceStore';
import { 
  Zap, 
  Plus, 
  Search, 
  Trash2, 
  Edit2,
  AlertCircle
} from 'lucide-react';
import { Account, AccountType, JournalEntry, JournalLine } from '../types';

export function ChartOfAccounts() {
  const { accounts, addAccount, deleteAccount, journalEntries } = useFinanceStore();
  const [isAdding, setIsAdding] = useState(false);
  const [newAccount, setNewAccount] = useState({ code: '', name: '', type: 'space' as AccountType });
  const [searchTerm, setSearchTerm] = useState('');

  const accountStats = useMemo(() => {
    const stats: Record<string, { debit: number, credit: number }> = {};
    accounts.forEach((a: Account) => stats[a.id] = { debit: 0, credit: 0 });

    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach((line: JournalLine) => {
        if (stats[line.accountId]) {
          stats[line.accountId].debit += line.debit;
          stats[line.accountId].credit += line.credit;
        }
      });
    });

    return stats;
  }, [accounts, journalEntries]);

  const filteredAccounts = accounts.filter((a: Account) => {
    const search = searchTerm.toLowerCase();
    return a.name.toLowerCase().includes(search) || a.code.includes(search);
  }).sort((a: Account, b: Account) => a.code.localeCompare(b.code));

  const handleAddAccount = () => {
    if (!newAccount.code || !newAccount.name) return;
    // Validate 4-digit numeric code
    if (!/^\d{4}$/.test(newAccount.code)) {
      alert('El código debe ser numérico de 4 dígitos (ej: 5721)');
      return;
    }
    addAccount({ ...newAccount, active: true });
    setIsAdding(false);
    setNewAccount({ code: '', name: '', type: 'space' });
  };

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  return (
    <div className="flex flex-col gap-8">
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-1">
          <h2 className="text-lg font-medium tracking-tight text-text-primary flex items-center gap-2">
            <Zap className="w-4 h-4 text-primary-orange" />
            Plan Contable
          </h2>
          <p className="text-xs text-text-secondary">Gestión de cuentas y estructura financiera</p>
        </div>
        <button 
          onClick={() => setIsAdding(true)}
          className="bg-primary-green text-background px-4 py-1.5 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all flex items-center gap-2"
        >
          <Plus className="w-3.5 h-3.5" /> Nueva Cuenta
        </button>
      </div>

      {isAdding && (
        <div className="bg-surface border border-border p-6 rounded-sm animate-in fade-in slide-in-from-top-2 duration-300">
          <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-6">Crear Nueva Cuenta</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Código (4 dígitos)</label>
              <input 
                type="text" 
                value={newAccount.code}
                onChange={e => setNewAccount(prev => ({ ...prev, code: e.target.value.replace(/\D/g, '').slice(0, 4) }))}
                placeholder="Ej: 5721"
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Nombre</label>
              <input 
                type="text" 
                value={newAccount.name}
                onChange={e => setNewAccount(prev => ({ ...prev, name: e.target.value }))}
                placeholder="Ej: Banco Sabadell"
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
              />
            </div>
            <div className="flex flex-col gap-2">
              <label className="text-[10px] font-mono uppercase tracking-wider text-text-secondary">Tipo</label>
              <select 
                value={newAccount.type}
                onChange={e => setNewAccount(prev => ({ ...prev, type: e.target.value as AccountType }))}
                className="bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary-green outline-none"
              >
                <option value="space">ESPACIO</option>
                <option value="entity">ENTIDAD</option>
              </select>
            </div>
          </div>
          <div className="flex justify-end gap-3">
            <button onClick={() => setIsAdding(false)} className="px-6 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary hover:text-text-primary transition-all">Cancelar</button>
            <button onClick={handleAddAccount} className="bg-primary-green text-background px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary-green/90 transition-all">Crear Cuenta</button>
          </div>
        </div>
      )}

      <div className="flex items-center gap-4 bg-surface border border-border p-4 rounded-sm">
        <div className="flex-1 relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-text-secondary" />
          <input 
            type="text" 
            placeholder="Buscar por nombre o código..." 
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
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary">Código</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary">Nombre</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary">Tipo</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary text-right">Debe</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary text-right">Haber</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary text-right">Saldo</th>
              <th className="p-4 text-[10px] font-mono uppercase tracking-widest text-text-secondary text-right">Acciones</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/30">
            {filteredAccounts.map((a: Account) => {
              const stats = accountStats[a.id] || { debit: 0, credit: 0 };
              const balance = stats.debit - stats.credit;
              
              return (
                <tr key={a.id} className="hover:bg-surface-elevated/20 transition-colors group">
                  <td className="p-4 text-xs font-mono text-text-secondary">{a.code}</td>
                  <td className="p-4">
                    <div className="flex flex-col">
                      <span className="text-sm font-medium">{a.name}</span>
                      {a.isSystem && <span className="text-[8px] font-mono uppercase text-primary-orange">Sistema</span>}
                    </div>
                  </td>
                  <td className="p-4">
                    <span className="text-[10px] font-mono uppercase px-2 py-0.5 bg-surface-elevated border border-border rounded-full">
                      {a.type === 'main' ? 'PRINCIPAL' : 
                       a.type === 'space' ? 'ESPACIO' : 
                       a.type === 'entity' ? 'ENTIDAD' : 'OTROS'}
                    </span>
                  </td>
                  <td className="p-4 text-right text-xs font-mono">{formatCurrency(stats.debit)}</td>
                  <td className="p-4 text-right text-xs font-mono">{formatCurrency(stats.credit)}</td>
                  <td className={`p-4 text-right text-sm font-bold numeric ${balance >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                    {formatCurrency(balance)}
                  </td>
                  <td className="p-4 text-right">
                    {!a.isSystem && (
                      <div className="flex items-center justify-end gap-2">
                        <button className="p-1.5 text-text-secondary hover:text-text-primary transition-all"><Edit2 className="w-3.5 h-3.5" /></button>
                        <button onClick={() => deleteAccount(a.id)} className="p-1.5 text-text-secondary hover:text-primary-orange transition-all"><Trash2 className="w-3.5 h-3.5" /></button>
                      </div>
                    )}
                  </td>
                </tr>
              );
            })}
            {filteredAccounts.length === 0 && (
              <tr key="empty-coa">
                <td colSpan={7} className="p-12 text-center text-text-secondary italic text-xs">
                  No se han encontrado cuentas.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
