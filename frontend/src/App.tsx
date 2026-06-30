import { useMemo, useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useFinanceStore } from './store/useFinanceStore';
import { Layout } from './components/Layout';
import { Dashboard } from './components/Dashboard';
import { BankStatement } from './components/BankStatement';
import { Spaces } from './components/Spaces';
import { Entities } from './components/Entities';
import { Journal } from './components/Journal';
import { ChartOfAccounts } from './components/ChartOfAccounts';
import { JournalEntry } from './types';
import { useAuth } from "react-oidc-context";
import { setAccessToken } from './api';

export default function App() {
  const auth = useAuth();
  const { accounts, journalEntries, fetchData, isLoading, error } = useFinanceStore();

  useEffect(() => {
    setAccessToken(auth.user?.access_token);
  }, [auth.user]);

  useEffect(() => {
    const isCallback = window.location.pathname === "/callback";
    if (!auth.isLoading && !auth.isAuthenticated && !isCallback) {
      auth.signinRedirect();
    }
  }, [auth.isLoading, auth.isAuthenticated]);

  useEffect(() => {
    if (auth.isAuthenticated) {
      fetchData();
    }
  }, [auth.isAuthenticated]);

  const handleLogout = async () => {
    await auth.signoutRedirect();
  };

  const treasuryMetrics = useMemo(() => {
    const accountBalances: Record<string, number> = {};
    accounts.forEach(a => accountBalances[a.id] = 0);

    journalEntries.forEach((entry: JournalEntry) => {
      entry.lines.forEach(line => {
        if (accountBalances[line.accountId] !== undefined) {
          accountBalances[line.accountId] += (line.debit - line.credit);
        }
      });
    });

    const mainAccount = accounts.find(a => a.type === 'main');
    const spaceAccounts = accounts.filter(a => a.type === 'space');

    const totalCommitted = spaceAccounts
      .reduce((sum, a) => sum + accountBalances[a.id], 0);

    const mainBalance = mainAccount ? accountBalances[mainAccount.id] : 0;
    const realBankBalance = mainBalance + totalCommitted;
    const availableCash = mainBalance;

    return {
      realBankBalance,
      totalCommitted,
      availableCash,
      bucketBalances: accountBalances
    };
  }, [accounts, journalEntries]);

  if (auth.isLoading || !auth.isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-orange mx-auto"></div>
          <p className="mt-4 text-text-secondary font-mono text-xs uppercase tracking-widest">Cargando...</p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-orange mx-auto"></div>
          <p className="mt-4 text-text-secondary font-mono text-xs uppercase tracking-widest">Cargando datos...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background p-6">
        <div className="text-center max-w-md border border-border p-12 bg-surface rounded-sm">
          <div className="text-primary-orange text-4xl mb-6">⚠️</div>
          <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-text-secondary mb-4">Error de conexión</h2>
          <p className="text-sm text-text-primary mb-8 leading-relaxed font-mono">{error}</p>
          <button
            onClick={() => fetchData()}
            className="px-8 py-3 bg-primary-orange text-white text-xs font-bold uppercase tracking-widest hover:bg-primary-orange/90 transition-all rounded-sm"
          >
            Reintentar
          </button>
        </div>
      </div>
    );
  }

  return (
    <BrowserRouter>
      <Layout onLogout={handleLogout}>
        <Routes>
          <Route path="/" element={<Dashboard metrics={treasuryMetrics} />} />
          <Route path="/bank" element={<BankStatement />} />
          <Route path="/spaces" element={<Spaces bucketBalances={treasuryMetrics.bucketBalances} />} />
          <Route path="/entities" element={<Entities />} />
          <Route path="/journal" element={<Journal />} />
          <Route path="/coa" element={<ChartOfAccounts />} />
          <Route path="/callback" element={null} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}
