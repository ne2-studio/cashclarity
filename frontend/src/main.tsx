import {StrictMode} from 'react';
import {createRoot} from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import { AuthProvider } from "react-oidc-context";
import type { User } from "oidc-client-ts";
import { getEnv } from './runtimeConfig';

const oidcConfig = {
  authority: getEnv('VITE_OIDC_AUTHORITY') || "https://auth.ne2.studio",
  client_id: getEnv('VITE_OIDC_CLIENT_ID') || "379616146464178946",
  redirect_uri: getEnv('VITE_OIDC_CALLBACK_URI') || "https://cashclarity-frontend.ne2.studio/callback",
  post_logout_redirect_uri: getEnv('VITE_OIDC_LOGOUT_REDIRECT_URI') || "https://cashclarity-frontend.ne2.studio",
  response_type: "code",
  // Enable Banking también vuelve con ?code&state; no son de OIDC y no deben procesarse como login.
  skipSigninCallback: window.location.pathname === "/bank-link/callback",
  scope: "openid profile email offline_access urn:zitadel:iam:org:domain:primary:cashclarity",
  onSigninCallback: (user: User | undefined) => {
    const returnTo = (user?.state as { returnTo?: string } | undefined)?.returnTo;
    window.history.replaceState({}, document.title, returnTo ?? "/");
  },
};

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider {...oidcConfig}>
      <App />
    </AuthProvider>
  </StrictMode>,
);
