import type { BankLink } from '../api';

export const CONSENT_WARNING_DAYS = 14;

export type ConsentState = 'ok' | 'expiring' | 'expired';

export function daysUntil(isoDate: string, now: Date = new Date()): number {
  return Math.floor((new Date(isoDate).getTime() - now.getTime()) / 86_400_000);
}

export function consentState(link: BankLink, now: Date = new Date()): ConsentState {
  if (link.status === 'expired' || (link.validUntil && new Date(link.validUntil) <= now)) return 'expired';
  if (link.validUntil && daysUntil(link.validUntil, now) < CONSENT_WARNING_DAYS) return 'expiring';
  return 'ok';
}

export function maskIban(iban?: string | null): string {
  if (!iban) return '';
  const compact = iban.replace(/\s+/g, '');
  return compact.length <= 8 ? compact : `${compact.slice(0, 4)} •••• ${compact.slice(-4)}`;
}

export function formatDate(isoDate?: string | null): string {
  if (!isoDate) return '—';
  return new Intl.DateTimeFormat('es-ES', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(isoDate));
}

export function formatDateTime(isoDate?: string | null): string {
  if (!isoDate) return 'nunca';
  return new Intl.DateTimeFormat('es-ES', { dateStyle: 'short', timeStyle: 'short' }).format(new Date(isoDate));
}
