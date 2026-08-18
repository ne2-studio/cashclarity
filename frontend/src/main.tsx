import {StrictMode} from 'react';
import {createRoot} from 'react-dom/client';
import App from './App.tsx';
import './index.css';
import { AuthProvider } from "react-oidc-context";
import { getEnv } from './runtimeConfig';

const oidcConfig = {
  authority: getEnv('VITE_OIDC_AUTHORITY') || "https://auth.ne2.studio",
  client_id: getEnv('VITE_OIDC_CLIENT_ID') || "379616146464178946",
  redirect_uri: getEnv('VITE_OIDC_CALLBACK_URI') || "https://cashclarity-frontend.ne2.studio/callback",
  post_logout_redirect_uri: getEnv('VITE_OIDC_LOGOUT_REDIRECT_URI') || "https://cashclarity-frontend.ne2.studio",
  response_type: "code",
  scope: "openid profile email offline_access urn:zitadel:iam:org:domain:primary:cashclarity",
  onSigninCallback: () => {
    window.history.replaceState({}, document.title, "/");
  },
};

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <AuthProvider {...oidcConfig}>
      <App />
    </AuthProvider>
  </StrictMode>,
);
