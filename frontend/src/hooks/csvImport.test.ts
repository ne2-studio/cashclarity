import { describe, expect, it } from 'vitest';
import { parseBankMovementsCsv } from './csvImport';

describe('parseBankMovementsCsv', () => {
  it('parses semicolon csv with quoted headers and decimal commas', () => {
    const result = parseBankMovementsCsv('"Fecha";"Concepto";"Cantidad"\r\n2026-01-02;"Factura";"123,45"');

    expect(result).toEqual({
      ok: true,
      movements: [{ date: '2026-01-02', description: 'Factura', amount: 123.45 }],
    });
  });

  it('returns detected headers when required columns are missing', () => {
    const result = parseBankMovementsCsv('fecha;detalle;importe\n2026-01-02;x;1');

    expect(result).toEqual({
      ok: false,
      error: 'Formato de CSV inválido. Debe contener las columnas: fecha; Concepto; cantidad',
      headers: ['fecha', 'detalle', 'importe'],
    });
  });

  it('rejects files without valid movements', () => {
    expect(parseBankMovementsCsv('fecha;concepto;cantidad\n; ;1')).toEqual({
      ok: false,
      error: 'No se han podido extraer movimientos válidos del archivo. Revisa el formato.',
    });
  });
});
