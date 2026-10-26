# Servidor MCP

La API expone un servidor [MCP](https://modelcontextprotocol.io) en `POST /mcp`. Usa el SDK
oficial de C# (`ModelContextProtocol.AspNetCore`) con transporte HTTP streamable sin estado. Así un
agente (Claude Code u otro cliente MCP) puede leer y modificar los datos de CashClarity del usuario
que ha iniciado sesión.

Manual de usuario (Zitadel + Claude Code): [`docs/user/mcp-claude-code.md`](../user/mcp-claude-code.md).

## Diseño

- **Una tool por endpoint REST.** Las tools viven en `backend/CashClarity.Api/Mcp/` y llaman a los
  mismos repositorios y servicios que los controladores. No contienen lógica de negocio.
- **Misma autenticación que la API.** `/mcp` exige un access token OIDC (JWT) emitido por
  `Auth:Authority` con audiencia `Auth:Audience`, validado por el mismo esquema `JwtBearer`. El
  `UserId` sale del claim `sub`, igual que en `BaseController`.
- **Descubrimiento OAuth.** Una petición anónima recibe `401` con
  `WWW-Authenticate: Bearer resource_metadata=...`. El documento
  `/.well-known/oauth-protected-resource/mcp` (RFC 9728) apunta a `Auth:Authority`, y desde ahí el
  cliente MCP inicia el login en el navegador.
- **Workspaces.** Las tools de cuentas, asientos y movimientos piden `workspaceId`. Igual que
  `WorkspaceOwnershipFilter`, un workspace que no es del usuario (o un id mal formado) devuelve
  `404`.
- **Errores.** Las excepciones de dominio se traducen con `ApiExceptionHandler.Map`. La tool
  devuelve `isError: true` con el texto `"<status>: <mensaje>"`, el mismo status que daría el
  endpoint REST (`400`, `404`...).
- **Validación de entrada.** Los esquemas JSON se generan a partir de las firmas C#. `type` de
  cuenta (`main`, `space`, `entity`, `uncategorized`) y `duplicatePolicy` (`skip`,
  `import-anyway`) son enums cerrados.
- **Sin stdio nativo.** El servidor vive dentro de la API. Los clientes que solo hablan stdio
  pueden usar el proxy [`mcp-remote`](https://www.npmjs.com/package/mcp-remote).
- `CashClarity.Api.Lite` no expone MCP.

## Tabla endpoint → tool

| Endpoint REST | Tool MCP | Tipo |
|---|---|---|
| `GET /server/health` | `health_check` | lectura |
| `GET /server/workspaces` | `list_workspaces` | lectura |
| `POST /server/workspaces` | `create_workspace` | escritura |
| `GET /server/workspaces/{workspaceId}/accounts` | `list_accounts` | lectura |
| `POST /server/workspaces/{workspaceId}/accounts` | `create_account` | escritura |
| `PATCH /server/workspaces/{workspaceId}/accounts/{id}` | `update_account` | escritura |
| `DELETE /server/workspaces/{workspaceId}/accounts/{id}` | `delete_account` | destructiva |
| `GET /server/workspaces/{workspaceId}/journal-entries` | `list_journal_entries` | lectura |
| `POST /server/workspaces/{workspaceId}/journal-entries` | `create_journal_entry` | escritura |
| `PATCH /server/workspaces/{workspaceId}/journal-entries/{id}` | `update_journal_entry` | destructiva (sustituye líneas) |
| `DELETE /server/workspaces/{workspaceId}/journal-entries/{id}` | `delete_journal_entry` | destructiva |
| `GET /server/workspaces/{workspaceId}/bank-movements` | `list_bank_movements` | lectura |
| `POST /server/workspaces/{workspaceId}/bank-movements` | `create_bank_movement` | escritura |
| `PATCH /server/workspaces/{workspaceId}/bank-movements/{id}` | `update_bank_movement` | escritura |
| `DELETE /server/workspaces/{workspaceId}/bank-movements/{id}` | `delete_bank_movement` | destructiva |
| `POST /server/workspaces/{workspaceId}/bank-movements/imports/preview` | `preview_bank_movement_import` | lectura (`csvText` o `fileBase64`) |
| `POST /server/workspaces/{workspaceId}/bank-movements/imports` | `commit_bank_movement_import` | escritura |
| `GET /server/workspaces/{workspaceId}/bank-link` | `get_bank_link` | lectura |
| `GET /server/workspaces/{workspaceId}/bank-link/aspsps` | `list_bank_aspsps` | lectura (Enable Banking) |
| `POST /server/workspaces/{workspaceId}/bank-link/authorizations` | `start_bank_authorization` | escritura (devuelve la URL de login del banco) |
| `POST /server/workspaces/{workspaceId}/bank-link/sessions` | `complete_bank_authorization` | escritura |
| `POST /server/workspaces/{workspaceId}/bank-link/account` | `select_bank_account` | escritura |
| `POST /server/workspaces/{workspaceId}/bank-link/sync` | `sync_bank_link` | escritura idempotente |
| `DELETE /server/workspaces/{workspaceId}/bank-link` | `unlink_bank` | destructiva (conserva movimientos) |

Espacios (huchas), entidades y saldo comprometido no tienen endpoint propio. Son cuentas con
`type` `space` o `entity`, y el saldo se deriva de los asientos. La descripción de `list_accounts`
explica al agente cómo calcularlo.

`McpServerTests.Every_rest_endpoint_has_an_mcp_tool` falla si se añade una acción a un controlador
sin su tool.

## Configuración del backend

| Clave | Obligatoria | Descripción |
|---|---|---|
| `Auth:Authority` | sí (ya existe) | Proveedor OIDC. Se publica como `authorization_servers`. |
| `Auth:Audience` | sí (ya existe) | Audiencia del JWT. Es el id del proyecto de Zitadel, así que vale para cualquier aplicación del proyecto. |
| `Mcp:ResourceUrl` | recomendada en producción | URL pública del endpoint, p. ej. `https://<host-de-la-api>/mcp`. Si falta, se infiere de la petición, y detrás de un proxy TLS puede salir `http://`. |
| `Mcp:Scopes` | no | Scopes anunciados. Por defecto `openid`, `profile`, `offline_access`. |

Como variables de entorno: `Mcp__ResourceUrl`, `Mcp__Scopes__0`, ...

## Tests

| Test | Qué cubre |
|---|---|
| `backend/CashClarity.Api.Tests/McpServerTests.cs` | Cliente MCP real contra `TestServer` con repositorios en memoria: inventario de tools, esquemas, recorridos de todas las tools, errores 400/404, workspaces ajenos, aislamiento por usuario y challenge 401. |
| `backend/acceptance-tests/.../McpTests.cs` | Imagen Docker real + fake OIDC: `401` con metadata y `create_workspace` + `list_accounts` con un JWT real. |

```bash
./scripts/verify backend
./scripts/verify backend-acceptance
```
