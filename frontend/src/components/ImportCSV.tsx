import React, { useState, ChangeEvent } from 'react';

import { Button, Modal } from '../design-system';
import type { BankMovementImportCommitResult, BankMovementImportPreview, BankMovementImportRow, DuplicatePolicy } from '../api';

interface ImportCSVProps {
  onClose: () => void;
  onPreview: (file: File) => Promise<BankMovementImportPreview>;
  onCommit: (rows: BankMovementImportRow[], duplicatePolicy?: DuplicatePolicy) => Promise<BankMovementImportCommitResult>;
}

export function ImportCSV({ onClose, onPreview, onCommit }: ImportCSVProps) {
  const [importPreview, setImportPreview] = useState<BankMovementImportPreview | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const handleFileChange = async (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    setIsLoading(true);
    setError(null);
    try {
      setImportPreview(await onPreview(file));
    } catch (err) {
      setImportPreview(null);
      setError((err as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  const confirmImport = async () => {
    if (!importPreview) return;
    setIsLoading(true);
    setError(null);
    try {
      await onCommit(importPreview.rows.filter(row => row.status !== 'invalid'), 'skip');
      onClose();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setIsLoading(false);
    }
  };

  const validCount = importPreview?.summary.valid ?? 0;
  const duplicateCount = importPreview?.summary.duplicates ?? 0;
  const importableCount = validCount;

  return (
    <Modal
      title="Importar Movimientos"
      onClose={onClose}
      width="2xl"
      footer={(
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button onClick={confirmImport} disabled={importableCount === 0 || isLoading}>
            Importar {importableCount > 0 ? importableCount : ''} Movimientos
          </Button>
        </>
      )}
    >
          <div className="flex flex-col gap-4">
            <p className="text-xs text-text-secondary">
              Selecciona un archivo CSV o Excel con columnas: <span className="font-mono font-bold">Fecha; Concepto/Movimiento; Cantidad/Importe</span>
            </p>
            <input 
              type="file" 
              accept=".csv,.xls,.xlsx"
              onChange={handleFileChange}
              className="block w-full text-xs text-text-secondary
                file:mr-4 file:py-2 file:px-4
                file:rounded-sm file:border-0
                file:text-[10px] file:font-mono file:uppercase file:tracking-widest
                file:bg-surface-elevated file:text-text-primary
                hover:file:bg-border transition-all"
            />
            {isLoading && <p className="text-[10px] text-text-secondary">Procesando CSV...</p>}
            {error && <p className="text-[10px] text-primary-orange">{error}</p>}
          </div>

          {importPreview && (
            <div className="flex flex-col gap-4">
              <div className="max-h-60 overflow-y-auto border border-border rounded-sm">
                <table className="w-full text-left">
                  <thead className="sticky top-0 bg-surface-elevated">
                    <tr className="border-b border-border">
                      <th className="p-2 text-[9px] font-mono uppercase text-text-secondary">Fecha</th>
                      <th className="p-2 text-[9px] font-mono uppercase text-text-secondary">Concepto</th>
                      <th className="p-2 text-[9px] font-mono uppercase text-text-secondary">Estado</th>
                      <th className="p-2 text-[9px] font-mono uppercase text-text-secondary text-right">Importe</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/30">
                    {importPreview.rows.map((m) => (
                      <tr key={m.rowNumber} className={m.status === 'invalid' || m.status === 'duplicate' ? 'opacity-60' : undefined}>
                        <td className="p-2 text-[10px] font-mono text-text-secondary">{m.date ?? '-'}</td>
                        <td className="p-2 text-[10px] font-medium">{m.description ?? m.errors.join(', ')}</td>
                        <td className="p-2 text-[10px] font-mono text-text-secondary">{statusText(m)}</td>
                        <td className={`p-2 text-[10px] font-mono text-right ${(m.amount ?? 0) >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                          {typeof m.amount === 'number' ? formatCurrency(m.amount) : '-'}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="text-[10px] text-text-secondary italic">
                Se han detectado {validCount} movimientos válidos.
                {duplicateCount > 0 ? ` ${duplicateCount} duplicados se omitiran.` : ''}
                {importPreview.summary.invalid > 0 ? ` ${importPreview.summary.invalid} filas invalidas.` : ''}
              </p>
            </div>
          )}
    </Modal>
  );
}

function statusText(row: BankMovementImportRow) {
  if (row.status === 'duplicate') return 'Duplicado';
  if (row.status === 'invalid') return 'Invalido';
  if (row.status === 'warning') return 'Aviso';
  return 'Valido';
}
