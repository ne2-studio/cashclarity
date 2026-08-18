import React, { useState, ChangeEvent } from 'react';

import { BankMovement } from '../types';
import { parseBankMovementsCsv } from '../hooks/csvImport';
import { Button, Modal } from '../design-system';

interface ImportCSVProps {
  onClose: () => void;
  onAddBankMovement: (movement: Omit<BankMovement, 'id' | 'isIdentified'>) => Promise<BankMovement>;
}

export function ImportCSV({ onClose, onAddBankMovement }: ImportCSVProps) {
  const [importPreview, setImportPreview] = useState<Omit<BankMovement, 'id' | 'isIdentified'>[]>([]);

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      const text = event.target?.result as string;
      const result = parseBankMovementsCsv(text);
      if ('movements' in result) {
        setImportPreview(result.movements);
        return;
      }

      if (result.headers) {
        console.log('Headers detectados:', result.headers);
        alert(`${result.error}\n\nHeaders encontrados: ${result.headers.join(', ')}`);
      } else {
        alert(result.error);
      }
    };
    reader.readAsText(file);
  };

  const confirmImport = () => {
    importPreview.forEach(m => onAddBankMovement(m));
    onClose();
  };

  return (
    <Modal
      title="Importar Movimientos CSV"
      onClose={onClose}
      width="2xl"
      footer={(
        <>
          <Button variant="ghost" onClick={onClose}>Cancelar</Button>
          <Button onClick={confirmImport} disabled={importPreview.length === 0}>
            Importar {importPreview.length > 0 ? importPreview.length : ''} Movimientos
          </Button>
        </>
      )}
    >
          <div className="flex flex-col gap-4">
            <p className="text-xs text-text-secondary">
              Selecciona un archivo CSV con formato separado por punto y coma (;) y las columnas: <span className="font-mono font-bold">fecha; Concepto; cantidad</span>
            </p>
            <input 
              type="file" 
              accept=".csv"
              onChange={handleFileChange}
              className="block w-full text-xs text-text-secondary
                file:mr-4 file:py-2 file:px-4
                file:rounded-sm file:border-0
                file:text-[10px] file:font-mono file:uppercase file:tracking-widest
                file:bg-surface-elevated file:text-text-primary
                hover:file:bg-border transition-all"
            />
          </div>

          {importPreview.length > 0 && (
            <div className="flex flex-col gap-4">
              <div className="max-h-60 overflow-y-auto border border-border rounded-sm">
                <table className="w-full text-left">
                  <thead className="sticky top-0 bg-surface-elevated">
                    <tr className="border-b border-border">
                      <th className="p-2 text-[9px] font-mono uppercase text-text-secondary">Fecha</th>
                      <th className="p-2 text-[9px] font-mono uppercase text-text-secondary">Concepto</th>
                      <th className="p-2 text-[9px] font-mono uppercase text-text-secondary text-right">Importe</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-border/30">
                    {importPreview.map((m, i) => (
                      <tr key={i}>
                        <td className="p-2 text-[10px] font-mono text-text-secondary">{m.date}</td>
                        <td className="p-2 text-[10px] font-medium">{m.description}</td>
                        <td className={`p-2 text-[10px] font-mono text-right ${m.amount >= 0 ? 'text-primary-green' : 'text-primary-orange'}`}>
                          {formatCurrency(m.amount)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              <p className="text-[10px] text-text-secondary italic">Se han detectado {importPreview.length} movimientos válidos.</p>
            </div>
          )}
    </Modal>
  );
}
