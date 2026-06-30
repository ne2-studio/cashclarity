import React, { useState } from 'react';
import { ShieldCheck, Lock, Mail, AlertCircle, UserPlus, LogIn } from 'lucide-react';
import { useAuth } from "react-oidc-context";

export function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [isRegistering, setIsRegistering] = useState(false);
  const [successMsg, setSuccessMsg] = useState('');

  const auth = useAuth();

  if (auth.isLoading) return (<p>Cargando...</p>);

  if (auth.error) return (<p>Error: {auth.error.message}</p>);

  return (
    <div className="min-h-screen bg-background flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        <div className="flex flex-col items-center gap-6 mb-12">
          <div className="w-16 h-16 bg-surface border border-border rounded-2xl flex items-center justify-center shadow-xl">
            <ShieldCheck className="w-10 h-10 text-primary-orange" />
          </div>
          <div className="text-center">
            <h1 className="text-2xl font-bold tracking-tighter uppercase font-mono mb-2">CashClarity</h1>
          </div>
        </div>

        <button onClick={() => auth.signinRedirect()}>
          Entrar con CashClarity
        </button>

        <div className="mt-8 text-center">
          <p className="text-[9px] font-mono text-text-secondary uppercase tracking-widest opacity-40">
            Acceso Restringido // Terminal de Control Financiero
          </p>
        </div>
      </div>
    </div>
  );
}
