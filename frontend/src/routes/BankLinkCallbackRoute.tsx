import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { api } from '../api';

/**
 * Destino de redirect_url de Enable Banking: el banco vuelve aquí con ?code&state (o ?error)
 * y el frontend, ya autenticado, completa la vinculación en el backend.
 */
export function BankLinkCallbackRoute() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;

    const code = params.get('code');
    const state = params.get('state');
    const bankError = params.get('error_description') ?? params.get('error');
    if (bankError || !code || !state) {
      setError(bankError ?? 'El banco no ha devuelto un código de autorización.');
      return;
    }

    api.bankLink.completeAuthorization(code, state)
      .then(() => navigate('/bank', { replace: true }))
      .catch((err: Error) => setError(err.message));
  }, [params, navigate]);

  return (
    <div className="flex flex-col items-center gap-4 py-24 text-center">
      {error ? (
        <>
          <h2 className="text-[10px] font-mono uppercase tracking-[0.2em] text-text-secondary">No se ha podido vincular la cuenta</h2>
          <p className="text-sm text-primary-orange font-mono max-w-md">{error}</p>
          <Link to="/bank" className="text-xs font-bold uppercase tracking-widest text-primary-orange hover:underline">Volver al extracto</Link>
        </>
      ) : (
        <p className="text-xs font-mono uppercase tracking-widest text-text-secondary">Completando vinculación bancaria...</p>
      )}
    </div>
  );
}
