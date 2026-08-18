import type { BankMovement } from '../types';

export type CsvImportResult =
  | { ok: true; movements: Omit<BankMovement, 'id' | 'isIdentified'>[] }
  | { ok: false; error: string; headers?: string[] };

export function parseBankMovementsCsv(text: string): CsvImportResult {
  const lines = text.split(/\r?\n/);
  if (lines.length < 2) {
    return { ok: false, error: 'El archivo está vacío o no tiene suficientes líneas.' };
  }

  const headers = lines[0].toLowerCase().split(';').map(cleanCsvCell);
  const dateIdx = headers.indexOf('fecha');
  const descIdx = headers.indexOf('concepto');
  const amountIdx = headers.indexOf('cantidad');

  if (dateIdx === -1 || descIdx === -1 || amountIdx === -1) {
    return {
      ok: false,
      error: 'Formato de CSV inválido. Debe contener las columnas: fecha; Concepto; cantidad',
      headers,
    };
  }

  const movements = lines
    .slice(1)
    .filter((line) => line.trim() !== '')
    .map((line) => {
      const cleanParts = line.split(';').map(cleanCsvCell);
      return {
        date: cleanParts[dateIdx] || '',
        description: cleanParts[descIdx] || '',
        amount: parseFloat(cleanParts[amountIdx]?.replace(',', '.') || '0'),
      };
    })
    .filter((movement) => movement.date && movement.description);

  if (movements.length === 0) {
    return {
      ok: false,
      error: 'No se han podido extraer movimientos válidos del archivo. Revisa el formato.',
    };
  }

  return { ok: true, movements };
}

function cleanCsvCell(value: string) {
  return value.replace(/["']/g, '').trim();
}
