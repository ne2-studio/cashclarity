import { describe, expect, it } from 'vitest';
import { consentState, daysUntil, maskIban } from './bankLinkViews';

const now = new Date('2026-10-24T10:00:00Z');

describe('bank link views', () => {
  it('counts whole days until consent expiry', () => {
    expect(daysUntil('2026-11-03T10:00:00Z', now)).toBe(10);
  });

  it('flags consent that expires soon or already expired', () => {
    expect(consentState({ configured: true, status: 'active', validUntil: '2027-01-20T00:00:00Z' }, now)).toBe('ok');
    expect(consentState({ configured: true, status: 'active', validUntil: '2026-11-01T00:00:00Z' }, now)).toBe('expiring');
    expect(consentState({ configured: true, status: 'active', validUntil: '2026-10-24T09:00:00Z' }, now)).toBe('expired');
    expect(consentState({ configured: true, status: 'expired', validUntil: '2027-01-20T00:00:00Z' }, now)).toBe('expired');
  });

  it('masks the IBAN keeping country prefix and last digits', () => {
    expect(maskIban('ES91 2100 0418 4502 0005 1332')).toBe('ES91 •••• 1332');
    expect(maskIban(null)).toBe('');
  });
});
