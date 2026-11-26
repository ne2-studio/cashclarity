# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: 00-dashboard.visual.spec.ts >> dashboard visual baseline
- Location: acceptance-tests/00-dashboard.visual.spec.ts:4:1

# Error details

```
Error: expect(page).toHaveScreenshot(expected) failed

  2370 pixels (ratio 0.01 of all image pixels) are different.

  Snapshot: dashboard.png

Call log:
  - Expect "toHaveScreenshot(dashboard.png)" with timeout 5000ms
    - verifying given screenshot expectation
  - taking page screenshot
    - disabled all CSS animations
  - waiting for fonts to load...
  - fonts loaded
  - 2370 pixels (ratio 0.01 of all image pixels) are different.
  - waiting 100ms before taking screenshot
  - taking page screenshot
    - disabled all CSS animations
  - waiting for fonts to load...
  - fonts loaded
  - captured a stable screenshot
  - 2370 pixels (ratio 0.01 of all image pixels) are different.

```

# Page snapshot

```yaml
- generic [ref=f2e4]:
  - banner [ref=f2e5]:
    - heading "CashClarity" [level=1] [ref=f2e10]
    - button "Workspace" [ref=f2e13]:
      - generic [ref=f2e14]: Principal
  - generic [ref=f2e17]:
    - navigation [ref=f2e18]:
      - generic [ref=f2e19]:
        - link "Dashboard" [ref=f2e20] [cursor=pointer]:
          - /url: /
        - link "Extracto Bancario" [ref=f2e26] [cursor=pointer]:
          - /url: /bank
        - link "Espacios" [ref=f2e29] [cursor=pointer]:
          - /url: /spaces
        - link "Entidades" [ref=f2e33] [cursor=pointer]:
          - /url: /entities
        - link "Libro Diario" [ref=f2e39] [cursor=pointer]:
          - /url: /journal
        - link "Plan Contable" [ref=f2e42] [cursor=pointer]:
          - /url: /coa
      - button "Cerrar Sesión" [ref=f2e47]
      - generic [ref=f2e51]: © 2026 CashClarity - v1.0.0
    - main [ref=f2e52]:
      - generic [ref=f2e54]:
        - generic [ref=f2e55]:
          - heading "Visibilidad de Caja // Tesorería Real" [level=2] [ref=f2e57]
          - generic [ref=f2e59]:
            - generic [ref=f2e60]:
              - generic [ref=f2e61]: Saldo Bancario Real
              - generic [ref=f2e66]: 0,00 €
              - generic [ref=f2e67]: Total consolidado en bancos
            - generic [ref=f2e68]:
              - generic [ref=f2e69]: Saldo Comprometido
              - generic [ref=f2e74]: 0,00 €
              - generic [ref=f2e75]: Asignado a espacios de reserva
            - generic [ref=f2e76]:
              - generic [ref=f2e77]: Saldo Disponible
              - generic [ref=f2e82]: 0,00 €
              - generic [ref=f2e83]: Caja libre para operaciones
        - generic [ref=f2e84]:
          - heading "Espacios // Reservas y Provisiones" [level=2] [ref=f2e86]
          - generic [ref=f2e88]:
            - table [ref=f2e91]:
              - rowgroup [ref=f2e92]:
                - row [ref=f2e93]:
                  - columnheader "Espacio" [ref=f2e94]
                  - columnheader "Saldo Actual" [ref=f2e95]
              - rowgroup [ref=f2e96]:
                - row [ref=f2e97]:
                  - cell "Cuenta Principal" [ref=f2e98]
                  - cell "0,00 €" [ref=f2e104]
            - generic [ref=f2e106]:
              - heading "Análisis de Liquidez" [level=3] [ref=f2e107]
              - generic [ref=f2e108]:
                - generic [ref=f2e109]:
                  - generic [ref=f2e110]: Ratio de Reserva
                  - generic [ref=f2e111]: 0 %
                - generic [ref=f2e113]:
                  - generic [ref=f2e114]: Comprometido
                  - generic [ref=f2e117]: Disponible
```

# Test source

```ts
  1  | import { expect, test } from '@playwright/test';
  2  | import { collectUnexpectedBrowserErrors, loginAsAcceptanceUser } from './helpers';
  3  | 
  4  | test('dashboard visual baseline', async ({ page }, testInfo) => {
  5  |   const assertNoBrowserErrors = collectUnexpectedBrowserErrors(page, testInfo);
  6  | 
  7  |   await loginAsAcceptanceUser(page);
  8  | 
  9  |   await expect(page.getByRole('heading', { name: 'CashClarity' })).toBeVisible();
  10 |   await expect(page.getByText('Visibilidad de Caja')).toBeVisible();
> 11 |   await expect(page).toHaveScreenshot('dashboard.png', {
     |                      ^ Error: expect(page).toHaveScreenshot(expected) failed
  12 |     fullPage: true,
  13 |     animations: 'disabled',
  14 |   });
  15 |   assertNoBrowserErrors();
  16 | });
  17 | 
```