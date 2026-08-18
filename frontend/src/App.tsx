import { useEffect } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useFinanceStore } from './store/useFinanceStore';
import { AppLayout } from './routes/AppLayout';
import { DashboardRoute } from './routes/DashboardRoute';
import { BankStatementRoute } from './routes/BankStatementRoute';
import { SpacesRoute } from './routes/SpacesRoute';
import { EntitiesRoute } from './routes/EntitiesRoute';
import { JournalRoute } from './routes/JournalRoute';
import { ChartOfAccountsRoute } from './routes/ChartOfAccountsRoute';
import { useAuth } from "react-oidc-context";
import { setAccessToken } from './api';
import { getEnv } from './runtimeConfig';

export default function App() {
  const authDisabled = getEnv('VITE_AUTH_DISABLED') === 'true';
  const auth = useAuth();
  const { fetchData, isLoading, error } = useFinanceStore();

  useEffect(() => {
    setAccessToken(authDisabled ? 'acceptance-token' : auth.user?.access_token);
  }, [authDisabled, auth.user]);

  useEffect(() => {
    if (authDisabled) return;
    const isCallback = window.location.pathname === "/callback";
    if (!auth.isLoading && !auth.isAuthenticated && !isCallback) {
      auth.signinRedirect();
    }
  }, [authDisabled, auth.isLoading, auth.isAuthenticated]);

  useEffect(() => {
    if (authDisabled || auth.isAuthenticated) {
      fetchData();
    }
  }, [authDisabled, auth.isAuthenticated]);

  const handleLogout = async () => {
    if (authDisabled) return;
    await auth.signoutRedirect();
  };

  if (!authDisabled && (auth.isLoading || !auth.isAuthenticated)) {
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
      <AppLayout onLogout={handleLogout}>
        <Routes>
          <Route path="/" element={<DashboardRoute />} />
          <Route path="/bank" element={<BankStatementRoute />} />
          <Route path="/spaces" element={<SpacesRoute />} />
          <Route path="/entities" element={<EntitiesRoute />} />
          <Route path="/journal" element={<JournalRoute />} />
          <Route path="/coa" element={<ChartOfAccountsRoute />} />
          <Route path="/callback" element={null} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AppLayout>
    </BrowserRouter>
  );
}
