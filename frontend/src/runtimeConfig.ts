declare global {
  interface Window {
    __ENV__?: Record<string, string>;
  }
}

type RuntimeEnvKey =
  | 'VITE_API_URL'
  | 'VITE_AUTH_DISABLED'
  | 'VITE_OIDC_AUTHORITY'
  | 'VITE_OIDC_CLIENT_ID'
  | 'VITE_OIDC_CALLBACK_URI'
  | 'VITE_OIDC_LOGOUT_REDIRECT_URI';

export function getEnv(key: RuntimeEnvKey): string {
  const runtimeEnv = typeof window === 'undefined' ? undefined : window.__ENV__?.[key];
  return runtimeEnv || (import.meta.env[key] as string) || '';
}
