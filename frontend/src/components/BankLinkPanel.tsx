import { useState } from 'react';
import { AlertTriangle, Link2, RefreshCw, Unlink } from 'lucide-react';
import type { BankAspsp, BankAuthorizationStart, BankLink, BankSyncResult } from '../api';
import { Button, Card, Select } from '../design-system';
import { consentState, daysUntil, formatDate, formatDateTime, maskIban } from '../hooks/bankLinkViews';
import { LinkBankModal } from './LinkBankModal';

interface BankLinkPanelProps {
  link: BankLink | null;
  lastSync: BankSyncResult | null;
  error: string | null;
  isBusy: boolean;
  onLoadAspsps: () => Promise<BankAspsp[]>;
  onStartLink: (request: BankAuthorizationStart) => Promise<void>;
  onSelectAccount: (accountUid: string) => Promise<void>;
  onSync: () => Promise<void>;
  onUnlink: () => Promise<void>;
  formatCurrency: (value: number) => string;
}

export function BankLinkPanel({
  link,
  lastSync,
  error,
  isBusy,
  onLoadAspsps,
  onStartLink,
  onSelectAccount,
  onSync,
  onUnlink,
  formatCurrency,
}: BankLinkPanelProps) {
  const [isLinking, setIsLinking] = useState(false);
  const [accountUid, setAccountUid] = useState('');

  if (!link || !link.configured) return null;

  const consent = consentState(link);
  const handleUnlink = async () => {
    if (window.confirm('¿Desvincular la cuenta bancaria? Los movimientos ya sincronizados se conservan.')) await onUnlink();
  };

  return (
    <Card className="p-4 flex flex-col gap-3" data-testid="bank-link-panel">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div className="flex items-start gap-3">
          <Link2 className="w-4 h-4 mt-0.5 text-text-secondary" />
          <div className="flex flex-col gap-1">
            <h3 className="text-[10px] font-mono uppercase tracking-[0.2em] text-text-secondary">Sincronización bancaria</h3>
            {link.status === 'none' && (
              <p className="text-xs text-text-secondary">Vincula tu cuenta para traer los movimientos sin exportar CSV.</p>
            )}
            {link.status === 'pending' && (
              <p className="text-xs text-text-secondary">Vinculación con {link.aspspName} pendiente de autorizar en el banco.</p>
            )}
            {link.status === 'awaiting-account' && (
              <p className="text-xs text-text-secondary">{link.aspspName} ha autorizado varias cuentas: elige cuál sincronizar.</p>
            )}
            {(link.status === 'active' || link.status === 'expired') && (
              <>
                <p className="text-sm font-medium">
                  {link.aspspName} · <span className="font-mono">{maskIban(link.iban)}</span>
                </p>
                <p className="text-[10px] font-mono uppercase tracking-widest text-text-secondary">
                  Última sync: {formatDateTime(link.lastSyncedAt)} · Desde {formatDate(link.syncFrom)}
                </p>
                <p className={`text-[10px] font-mono uppercase tracking-widest ${consent === 'ok' ? 'text-text-secondary' : 'text-primary-orange'}`}>
                  {consent === 'expired'
                    ? `Consentimiento caducado el ${formatDate(link.validUntil)}`
                    : `Consentimiento hasta ${formatDate(link.validUntil)} (${daysUntil(link.validUntil!)} días)`}
                </p>
              </>
            )}
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {link.status === 'none' && (
            <Button size="sm" onClick={() => setIsLinking(true)} icon={<Link2 className="w-3.5 h-3.5" />}>Vincular banco</Button>
          )}
          {link.status === 'pending' && (
            <>
              <Button size="sm" variant="secondary" onClick={() => setIsLinking(true)}>Reintentar</Button>
              <Button size="sm" variant="ghost" onClick={handleUnlink} disabled={isBusy}>Cancelar</Button>
            </>
          )}
          {link.status === 'awaiting-account' && (
            <>
              <Select aria-label="Cuenta bancaria" value={accountUid} onChange={(e) => setAccountUid(e.target.value)}>
                <option value="">Seleccionar cuenta...</option>
                {(link.accountOptions ?? []).map((option) => (
                  <option key={option.uid} value={option.uid}>
                    {option.name ?? 'Cuenta'} · {maskIban(option.iban)} {option.currency ?? ''}
                  </option>
                ))}
              </Select>
              <Button size="sm" onClick={() => onSelectAccount(accountUid)} disabled={!accountUid || isBusy}>Usar cuenta</Button>
            </>
          )}
          {link.status === 'active' && (
            <Button size="sm" onClick={onSync} disabled={isBusy} icon={<RefreshCw className={`w-3.5 h-3.5 ${isBusy ? 'animate-spin' : ''}`} />}>
              Sincronizar
            </Button>
          )}
          {(link.status === 'active' || link.status === 'expired') && (
            <>
              <Button size="sm" variant={consent === 'ok' ? 'secondary' : 'primary'} onClick={() => setIsLinking(true)}>
                {consent === 'expired' ? 'Volver a vincular' : 'Renovar consentimiento'}
              </Button>
              <Button size="sm" variant="ghost" onClick={handleUnlink} disabled={isBusy} icon={<Unlink className="w-3.5 h-3.5" />}>
                Desvincular
              </Button>
            </>
          )}
        </div>
      </div>

      {lastSync && (
        <div className="border-t border-border pt-3 flex flex-col gap-1 text-xs" data-testid="bank-sync-result">
          <p>
            {lastSync.created.length} movimientos nuevos · {lastSync.skippedExisting} ya existentes
            <span className="text-text-secondary"> ({formatDate(lastSync.dateFrom)} – {formatDate(lastSync.dateTo)})</span>
            {lastSync.balance && (
              <span className="text-text-secondary"> · Saldo en banco: <span className="numeric">{formatCurrency(lastSync.balance.amount)}</span></span>
            )}
          </p>
          {lastSync.possibleDuplicates.length > 0 && (
            <p className="flex items-center gap-2 text-primary-orange">
              <AlertTriangle className="w-3.5 h-3.5" />
              {lastSync.possibleDuplicates.length} movimientos nuevos coinciden en fecha e importe con otros ya importados. Revísalos y borra el duplicado.
            </p>
          )}
        </div>
      )}

      {error && <p className="text-xs text-primary-orange font-mono">{error}</p>}

      {isLinking && (
        <LinkBankModal
          link={link}
          onClose={() => setIsLinking(false)}
          onLoadAspsps={onLoadAspsps}
          onStart={onStartLink}
        />
      )}
    </Card>
  );
}
