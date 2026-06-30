import React, { useState, ChangeEvent } from 'react';
import { Upload, X, CheckCircle2, AlertCircle } from 'lucide-react';
import { useFinanceStore } from '../store/useFinanceStore';

import { BankMovement } from '../types';

interface ImportCSVProps {
  onClose: () => void;
}

export function ImportCSV({ onClose }: ImportCSVProps) {
  const { addBankMovement } = useFinanceStore();
  const [importPreview, setImportPreview] = useState<Omit<BankMovement, 'id' | 'isIdentified'>[]>([]);

  const formatCurrency = (val: number) => 
    new Intl.NumberFormat('es-ES', { style: 'currency', currency: 'EUR' }).format(val);

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const reader = new FileReader();
    reader.onload = (event) => {
      const text = event.target?.result as string;
      // Handle both \n and \r\n line endings
      const lines = text.split(/\r?\n/);
      if (lines.length < 2) {
        alert('El archivo está vacío o no tiene suficientes líneas.');
        return;
      }

      // Clean headers: remove quotes, spaces and convert to lowercase
      const headers = lines[0].toLowerCase().split(';').map(h => h.replace(/["']/g, '').trim());
      
      const dateIdx = headers.indexOf('fecha');
      const descIdx = headers.indexOf('concepto');
      const amountIdx = headers.indexOf('cantidad');

      if (dateIdx === -1 || descIdx === -1 || amountIdx === -1) {
        console.log('Headers detectados:', headers);
        alert('Formato de CSV inválido. Debe contener las columnas: fecha; Concepto; cantidad\n\nHeaders encontrados: ' + headers.join(', '));
        return;
      }

      const parsed = lines.slice(1)
        .filter(line => line.trim() !== '')
        .map(line => {
          const parts = line.split(';');
          // Clean each part from quotes and spaces
          const cleanParts = parts.map(p => p.replace(/["']/g, '').trim());
          
          return {
            date: cleanParts[dateIdx] || '',
            description: cleanParts[descIdx] || '',
            amount: parseFloat(cleanParts[amountIdx]?.replace(',', '.') || '0')
          };
        })
        .filter(m => m.date && m.description);

      if (parsed.length === 0) {
        alert('No se han podido extraer movimientos válidos del archivo. Revisa el formato.');
      }

      setImportPreview(parsed);
    };
    reader.readAsText(file);
  };

  const confirmImport = () => {
    importPreview.forEach(m => addBankMovement(m));
    onClose();
  };

  return (
    <div className="fixed inset-0 bg-background/80 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div className="bg-surface border border-border w-full max-w-2xl rounded-sm shadow-2xl animate-in zoom-in-95 duration-200">
        <div className="p-6 border-b border-border flex items-center justify-between bg-surface-elevated/20">
          <h3 className="text-sm font-bold uppercase tracking-widest">Importar Movimientos CSV</h3>
          <button onClick={onClose} className="text-text-secondary hover:text-text-primary">
            <X className="w-5 h-5" />
          </button>
        </div>
        <div className="p-8 flex flex-col gap-6">
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

          <div className="flex justify-end gap-3 pt-4 border-t border-border">
            <button onClick={onClose} className="px-6 py-2 text-xs font-bold uppercase tracking-widest text-text-secondary">Cancelar</button>
            <button 
              onClick={confirmImport} 
              disabled={importPreview.length === 0}
              className={`px-6 py-2 text-xs font-bold uppercase tracking-widest rounded-sm transition-all ${
                importPreview.length > 0 
                  ? 'bg-primary-green text-background hover:bg-primary-green/90' 
                  : 'bg-border text-text-secondary cursor-not-allowed'
              }`}
            >
              Importar {importPreview.length > 0 ? importPreview.length : ''} Movimientos
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
