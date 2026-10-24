import type { Meta, StoryObj } from '@storybook/react-vite';
import { expect, within } from 'storybook/test';
import { BankLinkPanel } from './BankLinkPanel';
import { BankMovement } from '../types';

const inDays = (days: number) => new Date(Date.now() + days * 86_400_000).toISOString();

const activeLink = {
  configured: true,
  status: 'active' as const,
  aspspName: 'CaixaBank',
  aspspCountry: 'ES',
  psuType: 'business' as const,
  iban: 'ES9121000418450200051332',
  accountName: 'Cuenta corriente',
  currency: 'EUR',
  validUntil: inDays(80),
  syncFrom: '2026-10-01',
  lastSyncedAt: '2026-10-24T10:00:00Z',
};

const meta = {
  title: 'Components/BankLinkPanel',
  component: BankLinkPanel,
  args: {
    link: activeLink,
    lastSync: null,
    error: null,
    isBusy: false,
    onLoadAspsps: async () => [{ name: 'CaixaBank', country: 'ES', psuTypes: ['personal', 'business'] }],
    onStartLink: async () => undefined,
    onSelectAccount: async () => undefined,
    onSync: async () => undefined,
    onUnlink: async () => undefined,
    formatCurrency: (value: number) => `${value.toFixed(2)} €`,
  },
} satisfies Meta<typeof BankLinkPanel>;

export default meta;
type Story = StoryObj<typeof meta>;

export const NotLinked: Story = {
  args: { link: { configured: true, status: 'none' } },
  play: async ({ canvasElement }) => {
    await expect(within(canvasElement).getByRole('button', { name: 'Vincular banco' })).toBeVisible();
  },
};

export const Active: Story = {
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByText('ES91 •••• 1332')).toBeVisible();
    await expect(canvas.getByRole('button', { name: 'Sincronizar' })).toBeEnabled();
  },
};

export const AfterSync: Story = {
  args: {
    lastSync: {
      created: [new BankMovement({ id: 'm1', date: '2026-10-20', description: 'Nomina', amount: 1500, isIdentified: false })],
      skippedExisting: 3,
      possibleDuplicates: [{ movementId: 'm1', existingMovementId: 'csv-1' }],
      dateFrom: '2026-10-17',
      dateTo: '2026-10-24',
      balance: { amount: 1234.56, currency: 'EUR', type: 'CLBD' },
      link: activeLink,
    },
  },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByText(/1 movimientos nuevos · 3 ya existentes/)).toBeVisible();
    await expect(canvas.getByText(/coinciden en fecha e importe/)).toBeVisible();
  },
};

export const ConsentExpiringSoon: Story = {
  args: { link: { ...activeLink, validUntil: inDays(5) } },
  play: async ({ canvasElement }) => {
    await expect(within(canvasElement).getByRole('button', { name: 'Renovar consentimiento' })).toBeVisible();
  },
};

export const ConsentExpired: Story = {
  args: { link: { ...activeLink, status: 'expired', validUntil: inDays(-1) } },
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    await expect(canvas.getByText(/Consentimiento caducado/)).toBeVisible();
    await expect(canvas.queryByRole('button', { name: 'Sincronizar' })).toBeNull();
    await expect(canvas.getByRole('button', { name: 'Volver a vincular' })).toBeVisible();
  },
};

export const AwaitingAccount: Story = {
  args: {
    link: {
      configured: true,
      status: 'awaiting-account',
      aspspName: 'N26',
      accountOptions: [
        { uid: 'uid-1', iban: 'ES9121000418450200051332', name: 'Principal', currency: 'EUR' },
        { uid: 'uid-2', iban: 'ES7620770024003102575766', name: 'Espacio', currency: 'EUR' },
      ],
    },
  },
};

export const NotConfigured: Story = {
  args: { link: { configured: false, status: 'none' } },
  play: async ({ canvasElement }) => {
    await expect(within(canvasElement).queryByTestId('bank-link-panel')).toBeNull();
  },
};
