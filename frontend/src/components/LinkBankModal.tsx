import { useEffect, useMemo, useState } from 'react';
import type { BankAspsp, BankAuthorizationStart, BankLink, PsuType } from '../api';
import { Button, FormField, Input, Modal, Select } from '../design-system';

interface LinkBankModalProps {
  link: BankLink | null;
  onClose: () => void;
  onLoadAspsps: () => Promise<BankAspsp[]>;
  onStart: (request: BankAuthorizationStart) => Promise<void>;
}

const PSU_LABELS: Record<PsuType, string> = { personal: 'Particular', business: 'Empresa' };

export function LinkBankModal({ link, onClose, onLoadAspsps, onStart }: LinkBankModalProps) {
  const relinking = Boolean(link?.iban);
  const [aspsps, setAspsps] = useState<BankAspsp[]>([]);
  const [aspspName, setAspspName] = useState(link?.aspspName ?? '');
  const [psuType, setPsuType] = useState<PsuType>(link?.psuType ?? 'personal');
  const [syncFrom, setSyncFrom] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    onLoadAspsps()
      .then(setAspsps)
      .catch((err: Error) => setError(err.message))
      .finally(() => setIsLoading(false));
  }, [onLoadAspsps]);

  const selected = useMemo(() => aspsps.find((a) => a.name === aspspName), [aspsps, aspspName]);
  const psuTypes = selected?.psuTypes.length ? selected.psuTypes : (['personal', 'business'] as PsuType[]);

  useEffect(() => {
    if (selected && !psuTypes.includes(psuType)) setPsuType(psuTypes[0]);
  }, [selected]);

  const handleStart = async () => {
    if (!selected) return;
    setIsLoading(true);
    setError(null);
    try {
      await onStart({ aspspName: selected.name, aspspCountry: selected.country, psuType, syncFrom: syncFrom || null });
    } catch (err) {
      setError((err as Error).message);
      setIsLoading(false);
    }
  };

  return (
    <Modal
      title={relinking ? 'Renovar consentimiento' : 'Vincular cuenta bancaria'}
      subtitle="Serás redirigido al banco para autorizar el acceso de solo lectura (PSD2, máx. 90 días)."
      onClose={onClose}
      footer={(
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button onClick={handleStart} disabled={!selected || isLoading}>Ir al banco</Button>
        </>
      )}
    >
      <div className="flex flex-col gap-4">
        <FormField label="Banco">
          <Select
            aria-label="Banco"
            value={aspspName}
            onChange={(e) => setAspspName(e.target.value)}
            disabled={relinking || isLoading}
            className="w-full"
          >
            <option value="">{isLoading ? 'Cargando bancos...' : 'Seleccionar banco...'}</option>
            {aspsps.map((a) => (
              <option key={`${a.country}-${a.name}`} value={a.name}>{a.name}</option>
            ))}
          </Select>
        </FormField>
        <FormField label="Tipo de cliente">
          <Select aria-label="Tipo de cliente" value={psuType} onChange={(e) => setPsuType(e.target.value as PsuType)} className="w-full">
            {psuTypes.map((type) => (
              <option key={type} value={type}>{PSU_LABELS[type]}</option>
            ))}
          </Select>
        </FormField>
        {!relinking && (
          <FormField label="Sincronizar desde">
            <Input aria-label="Sincronizar desde" type="date" value={syncFrom} onChange={(e) => setSyncFrom(e.target.value)} />
            <p className="text-[10px] text-text-secondary">
              Vacío: el día siguiente al último movimiento existente, para no solapar con lo ya importado por CSV.
            </p>
          </FormField>
        )}
        {error && <p className="text-xs text-primary-orange font-mono">{error}</p>}
      </div>
    </Modal>
  );
}
