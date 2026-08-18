import { useState } from 'react';
import { 
  Zap, 
  Plus, 
  Trash2, 
  Edit2,
} from 'lucide-react';
import { Account, AccountType, JournalEntry } from '../types';
import { isValidAccountCode, useChartOfAccountsViewModel } from '../hooks/accountViews';
import { Button, Card, FormField, IconButton, Input, PageHeader, SearchField, Select } from '../design-system';

interface ChartOfAccountsProps {
  accounts: Account[];
  journalEntries: JournalEntry[];
  onAddAccount: (account: Omit<Account, 'id'>) => Promise<Account>;
  onDeleteAccount: (id: string) => Promise<void>;
}

export function ChartOfAccounts({ accounts, journalEntries, onAddAccount, onDeleteAccount }: ChartOfAccountsProps) {
  const [isAdding, setIsAdding] = useState(false);
  const [newAccount, setNewAccount] = useState({ code: '', name: '', type: 'space' as AccountType });
  const [searchTerm, setSearchTerm] = useState('');
  const { accountStats, filteredAccounts } = useChartOfAccountsViewModel(accounts, journalEntries, searchTerm);

  const handleAddAccount = () => {
    if (!newAccount.code || !newAccount.name) return;
    // Validate 4-digit numeric code
    if (!isValidAccountCode(newAccount.code)) {
      alert('El código debe ser numérico de 4 dígitos (ej: 5721)');
      return;
    }
    onAddAccount({ ...newAccount, active: true });
    setIsAdding(false);
    setNewAccount({ code: '', name: '', type: 'space' });
  };

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  return (
    <div className="flex flex-col gap-8">
      <PageHeader
        icon={<Zap className="w-4 h-4 text-primary-orange" />}
        title="Plan Contable"
        subtitle="Gestión de cuentas y estructura financiera"
        actions={(
          <Button size="sm" onClick={() => setIsAdding(true)} icon={<Plus className="w-3.5 h-3.5" />}>
            Nueva Cuenta
          </Button>
        )}
      />

      {isAdding && (
        <Card className="p-6 animate-in fade-in slide-in-from-top-2 duration-300">
          <h3 className="text-[10px] font-mono uppercase tracking-widest text-text-secondary mb-6">Crear Nueva Cuenta</h3>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-8">
            <FormField label="Código (4 dígitos)">
              <Input
                type="text" 
                value={newAccount.code}
                onChange={e => setNewAccount(prev => ({ ...prev, code: e.target.value.replace(/\D/g, '').slice(0, 4) }))}
                placeholder="Ej: 5721"
              />
            </FormField>
            <FormField label="Nombre">
              <Input
                type="text" 
                value={newAccount.name}
                onChange={e => setNewAccount(prev => ({ ...prev, name: e.target.value }))}
                placeholder="Ej: Banco Sabadell"
              />
            </FormField>
            <FormField label="Tipo">
              <Select
                value={newAccount.type}
                onChange={e => setNewAccount(prev => ({ ...prev, type: e.target.value as AccountType }))}
              >
                <option value="space">ESPACIO</option>
                <option value="entity">ENTIDAD</option>
              </Select>
            </FormField>
          </div>
          <div className="flex justify-end gap-3">
            <Button variant="ghost" onClick={() => setIsAdding(false)}>Cancelar</Button>
            <Button onClick={handleAddAccount}>Crear Cuenta</Button>
          </div>
        </Card>
      )}

      <Card className="flex items-center gap-4 p-4">
        <SearchField
          className="flex-1"
          placeholder="Buscar por nombre o código..."
          value={searchTerm}
          onChange={e => setSearchTerm(e.target.value)}
        />
      </Card>

      <Card className="overflow-hidden">
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
                        <IconButton hover="default" aria-label="Editar cuenta"><Edit2 className="w-3.5 h-3.5" /></IconButton>
                        <IconButton onClick={() => onDeleteAccount(a.id)} aria-label="Eliminar cuenta"><Trash2 className="w-3.5 h-3.5" /></IconButton>
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
      </Card>
    </div>
  );
}
