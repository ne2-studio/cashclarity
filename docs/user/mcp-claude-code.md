# Usar CashClarity desde Claude Code (MCP)

CashClarity tiene un servidor MCP. Con él, Claude Code puede consultar y modificar tus cuentas,
espacios, asientos y movimientos bancarios (en cualquiera de tus workspaces) en tu nombre, con tu misma cuenta de usuario.

Hay dos partes:

1. **Administrador (una sola vez):** registrar Claude Code como aplicación en Zitadel y compartir el
   *Client ID*.
2. **Cada usuario:** añadir el servidor en Claude Code e iniciar sesión.

---

## 1. Administrador: registrar el cliente en Zitadel

El servidor MCP valida los mismos tokens que la web de CashClarity. Por eso la aplicación tiene
que crearse **en el mismo proyecto de Zitadel** que la web: así el token incluye la audiencia que
espera la API (`Auth:Audience`).

1. Entra en la consola de Zitadel (`https://auth.ne2.studio`) con un usuario administrador.
2. Abre **Projects** y selecciona el proyecto de CashClarity, el que tiene como id el valor de
   `Auth:Audience` del backend.
3. Pulsa **New** en *Applications*:
   - **Name:** `CashClarity MCP (Claude Code)`
   - **Type:** `Native`
   - **Authentication method:** `PKCE` (sin client secret)
4. En **Redirect URIs** añade exactamente:

   ```
   http://localhost:3118/callback
   ```

   El puerto `3118` es un acuerdo: todos los usuarios deben usar el mismo `--callback-port`. Si
   Zitadel rechaza la URI por ser `http`, activa **Development Mode** en la aplicación (solo afecta
   a la validación de URIs de redirección).
5. Crea la aplicación y entra en su configuración:
   - **Token Settings → Auth Token Type:** `JWT`. Es **obligatorio**: la API no acepta tokens
     opacos.
   - **Grant types:** marca `Authorization Code` y `Refresh Token`. El refresh token permite que
     Claude Code renueve la sesión sin pedir login cada vez que caduca el access token.
6. Copia el **Client ID** y compártelo con los usuarios. No es un secreto, pero tampoco hace falta
   publicarlo.

### Configuración del backend (una vez por entorno)

Define la URL pública del endpoint MCP en el despliegue de la API:

```
Mcp__ResourceUrl=https://<host-de-la-api>/mcp
```

Para comprobarlo:

```bash
curl https://<host-de-la-api>/.well-known/oauth-protected-resource/mcp
```

La respuesta debe incluir `"authorization_servers": ["https://auth.ne2.studio"]` y la `resource`
anterior.

---

## 2. Usuario: conectar Claude Code

Necesitas Claude Code instalado, una cuenta en CashClarity y el **Client ID** que te ha pasado el
administrador.

### Añadir el servidor

```bash
claude mcp add --transport http \
  --client-id <CLIENT_ID> \
  --callback-port 3118 \
  --scope user \
  cashclarity https://<host-de-la-api>/mcp
```

- `--scope user` deja el servidor disponible en todos tus proyectos. Para un solo proyecto, quítalo.
- No guardes el Client ID ni tokens en ficheros del repositorio.

### Iniciar sesión

1. Abre Claude Code y ejecuta `/mcp`.
2. Selecciona `cashclarity` → **Authenticate**.
3. Se abre el navegador con el login de CashClarity (Zitadel). Entra con tu usuario habitual.
4. Vuelve a Claude Code. `cashclarity` debe aparecer como *connected* con 17 tools.

Prueba rápida: pide a Claude *"lista mis espacios de CashClarity y dime cuánto tengo comprometido"*.
Claude primero llamará a `list_workspaces`; si tienes varios workspaces, dile cuál usar.

### Renovar la sesión

La sesión dura lo que permitan los tokens de Zitadel. Si una tool falla con *401* o el servidor
aparece como *needs authentication*, repite `/mcp` → `cashclarity` → **Authenticate**. Un agente
desatendido tiene que pasar periódicamente por este login humano.

### Qué puede hacer Claude

| Área | Tools |
|---|---|
| Workspaces (cada extracto bancario) | `list_workspaces`, `create_workspace` |
| Cuentas, espacios (huchas) y entidades | `list_accounts`, `create_account`, `update_account`, `delete_account` |
| Asientos contables | `list_journal_entries`, `create_journal_entry`, `update_journal_entry`, `delete_journal_entry` |
| Movimientos bancarios | `list_bank_movements`, `create_bank_movement`, `update_bank_movement`, `delete_bank_movement` |
| Importación de extractos | `preview_bank_movement_import`, `commit_bank_movement_import` |
| Diagnóstico | `health_check` |

Las tools de borrado están marcadas como destructivas. Claude Code te pedirá confirmación antes de
usarlas, salvo que las autorices de forma permanente.

### Clientes solo stdio

Si tu cliente MCP no soporta HTTP, usa el proxy `mcp-remote`. El administrador tiene que añadir
también su URI de redirección en Zitadel: `http://localhost:3118/oauth/callback`.

```json
{
  "mcpServers": {
    "cashclarity": {
      "command": "npx",
      "args": [
        "mcp-remote", "https://<host-de-la-api>/mcp", "3118",
        "--static-oauth-client-info", "{\"client_id\":\"<CLIENT_ID>\"}"
      ]
    }
  }
}
```

---

## Problemas frecuentes

| Síntoma | Causa probable | Solución |
|---|---|---|
| Zitadel: `redirect_uri` no válida | Puerto o ruta distintos de los registrados | Usa `--callback-port 3118` y revisa la URI en Zitadel. |
| Login correcto, pero las tools dan `401` | La aplicación de Zitadel emite tokens opacos o está en otro proyecto | Auth Token Type = `JWT` y app dentro del proyecto de CashClarity. |
| Claude Code no abre el navegador | No ha recibido el `WWW-Authenticate` | Comprueba `curl -i -X POST https://<host-de-la-api>/mcp` → `401` con `resource_metadata`. |
| La metadata muestra `http://` en `resource` | Falta `Mcp__ResourceUrl` detrás del proxy | Defínela en el despliegue. |
| Error `invalid_scope` | La app no permite refresh tokens | Activa el grant `Refresh Token` o configura `Mcp__Scopes` sin `offline_access`. |
