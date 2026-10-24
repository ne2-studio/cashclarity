# Sincronización bancaria con Enable Banking

CashClarity puede leer los movimientos de una cuenta bancaria real a través de
[Enable Banking](https://enablebanking.com/docs/api/reference/) (open banking PSD2),
en lugar de exportar un CSV del banco e importarlo.

## Modelo

- **Un vínculo bancario por libro.** Hoy el libro es el usuario (`user_id`); cuando existan
  workspaces, el vínculo pasará a colgar del workspace. Tabla `bank_links`, índice único por
  `user_id`.
- El vínculo guarda banco (`aspsp_name`, `aspsp_country`), tipo de cliente (`psu_type`:
  `personal` | `business`), `session_id`, `valid_until` (caducidad del consentimiento),
  cuenta elegida (`account_uid`, `iban`, `account_name`, `currency`), `sync_from` y
  `last_synced_at`.
- `session_id` se guarda en claro: sin la clave privada de la aplicación no da acceso a nada.
- Los movimientos sincronizados son `bank_movements` normales con `external_id` relleno. Llegan
  sin identificar (`is_identified = false`, sin entidad), igual que con la importación CSV.
  Índice único `(user_id, external_id)` cuando `external_id` no es nulo.

## Flujo de vinculación

1. *Extracto Bancario → Vincular banco*: el usuario elige banco (lista de `GET /aspsps?country=ES`),
   tipo de cliente y, opcionalmente, la fecha desde la que sincronizar. Si se deja vacía, se usa
   el día siguiente al último movimiento existente para no solapar con lo importado por CSV.
2. `POST /server/bank-link/authorizations`: el backend genera un `state` anti-CSRF (caduca en
   30 min), llama a `POST /auth` con `access.valid_until` = mín(máximo del banco, 90 días) y
   devuelve la URL de login del banco.
3. El banco redirige a `EnableBanking__RedirectUrl` (ruta del frontend `/bank-link/callback`)
   con `?code&state`. El frontend, autenticado con OIDC, envía ambos a
   `POST /server/bank-link/sessions`. El backend valida el `state` y crea la sesión
   (`POST /sessions`).
4. Si la sesión trae una sola cuenta, queda vinculada. Si trae varias, la UI pide elegir
   (`POST /server/bank-link/account`).

**Renovar o volver a vincular** repite el flujo con el mismo banco. Como los `uid` de cuenta
cambian en cada sesión, la cuenta se reconoce por IBAN. Si la nueva autorización no incluye ese
IBAN, se rechaza y la sesión anterior sigue activa. Para cambiar de banco o de cuenta hay que
desvincular primero (`DELETE /server/bank-link`), que cierra la sesión y conserva los movimientos.

## Sincronización

`POST /server/bank-link/sync` (botón *Sincronizar*):

- **Rango:** `date_from` = `sync_from` en la primera sync; después, la fecha de la última sync
  menos 7 días (los bancos contabilizan con retraso). El tope es `sync_from`. `date_to` = hoy.
- Pide `GET /accounts/{uid}/transactions?transaction_status=BOOK` siguiendo `continuation_key`.
  Solo se importan transacciones contabilizadas (`BOOK`), porque las pendientes cambian de
  identificador.
- **Mapeo:** fecha = `booking_date` (o `value_date` / `transaction_date`); importe con signo
  negativo si `credit_debit_indicator = DBIT`; concepto = `remittance_information` unido o, si
  falta, nombre de la contraparte.
- **Deduplicación:** `external_id` = `ref:{entry_reference}`; si falta, `tx:{transaction_id}`; si
  también falta, `hash:{sha256(fecha|importe|concepto normalizado)}`. Las repeticiones dentro de
  una misma respuesta (dos cafés iguales el mismo día) reciben un sufijo `#n` según su orden.
  Re-sincronizar es idempotente.
- **Aviso, sin bloqueo:** si un movimiento nuevo coincide en fecha e importe con uno manual o de
  CSV, la respuesta lo incluye en `possibleDuplicates` y la UI avisa para revisarlo.
- La respuesta incluye el saldo que informa el banco (`GET /accounts/{uid}/balances`) para
  conciliar.
- Consentimiento caducado (`valid_until` vencido) o sesión revocada en el banco
  (`EXPIRED_SESSION`, `CLOSED_SESSION`, `REVOKED_SESSION`): error 400 con el mensaje «vuelve a
  vincular». La UI muestra la fecha de caducidad y avisa cuando quedan menos de 14 días.

## Configuración y secretos

Solo por variables de entorno o ficheros fuera del repositorio. `.gitignore` excluye `*.pem`,
`*.key` y `secrets/`.

| Variable | Contenido |
|---|---|
| `EnableBanking__ApplicationId` | ID de la aplicación en Enable Banking (va como `kid` del JWT) |
| `EnableBanking__PrivateKeyPath` | Ruta al PEM de la clave privada, fuera del repo |
| `EnableBanking__PrivateKey` | Alternativa: el PEM como texto (secreto inyectado) |
| `EnableBanking__RedirectUrl` | URL https del frontend + `/bank-link/callback`, registrada en la app de Enable Banking |
| `EnableBanking__BaseUrl` | Opcional; por defecto `https://api.enablebanking.com` |

Sin estas variables, la API arranca igualmente y el panel de sincronización no aparece
(`configured: false`).

El backend firma un JWT RS256 por petición con `kid` = application id,
`iss = enablebanking.com`, `aud = api.enablebanking.com` y validez de 1 h.

### Probar en local con un banco real

`redirect_url` debe ser https y estar registrada en la aplicación de Enable Banking. Opciones:
registrar la URL del frontend desplegado, o servir el frontend local por https y registrar esa
URL. Ejemplo con la API real (`./scripts/run-env backend-dev`):

```bash
export EnableBanking__ApplicationId=<id>
export EnableBanking__PrivateKeyPath=$HOME/.config/cashclarity/enablebanking.pem
export EnableBanking__RedirectUrl=https://<frontend>/bank-link/callback
```

## Tests

- `CashClarity.Api.Tests/EnableBanking`: servicio y cliente reales contra una API de Enable
  Banking simulada a nivel HTTP (`MockEnableBankingApi`). Cubren la validación del JWT, el flujo
  de vinculación, dos syncs seguidas sin duplicados, la paginación, la caducidad, la
  re-vinculación por IBAN y el mapeo.
- `CashClarity.Api.PersistenceTests`: índice único de `external_id` y round-trip de `bank_links`
  en Postgres.
- `CashClarity.Api.Lite` usa `FakeEnableBankingClient`: el «login del banco» vuelve directamente
  al callback. Playwright cubre el journey de vincular y sincronizar dos veces.

## Bancos objetivo

CaixaBank (business), N26 (personal) y MyInvestor Banco. La disponibilidad real y los tipos de
cliente admitidos salen de `GET /aspsps` en tiempo de ejecución. Pendiente de validar con cuentas
reales: que cada banco devuelva `entry_reference` estable. Si no lo hace, se usa el hash de
contenido.
