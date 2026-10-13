# Contrato de API — Caja ↔ Integration

Documento para el equipo de **Integration** (Ricardo). Describe los endpoints que el módulo **Caja** ya consume. La URL base se configura en `CajaApp/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:7107/api",
    "UseMockAuthentication": false,
    "UseMockSubsidio": false
  },
  "Caja": {
    "Terminal": "CJ01"
  }
}
```

- Mientras `UseMockAuthentication` sea `true`, el login no llama a Integration (usuario de demostración `cajero` / `1234`).
- Mientras `UseMockSubsidio` sea `true`, la validación de subsidio consulta la tabla `Pacientes` local (datos de demostración).
- Para la prueba conjunta con el Gateway: poner la URL correcta en `BaseUrl` y cambiar **ambas** banderas a `false`. Entonces el login usa `cajero.prueba` del CORE y el subsidio aplica la regla del CORE.
- `Caja:Terminal` es el prefijo de numeración de los comprobantes de esta terminal (ver sección 5).

Convenciones:

- Formato: JSON (UTF-8), nombres de propiedad en **camelCase** (comportamiento por defecto de `System.Text.Json` en .NET).
- Fechas: ISO 8601 en hora local (`2026-10-07T18:07:12`).
- Montos: número decimal con 2 decimales (`1500.00`).
- Autenticación: después del login, cada envío de sincronización lleva `Authorization: Bearer <token>`.
- Timeout del cliente: 3 segundos. Si no hay respuesta, Caja sigue trabajando sin conexión y reintenta cada 30 segundos.

---

## 1. Login

`POST {BaseUrl}/auth/login`

Solicitud:

```json
{ "usuario": "cajero", "password": "1234" }
```

Respuesta `200 OK`:

```json
{
  "exito": true,
  "token": "eyJhbGciOi...",
  "roles": ["Farmacia", "Cajero"],
  "nombreCompleto": "María Rosario",
  "mensaje": "Autenticación correcta."
}
```

- Caja acepta `roles` (lista, formato del CORE) o `rol` (texto o lista, formato anterior).
- Si el usuario tiene varios roles, Caja entra con uno solo, por prioridad: `Administrador` (también `Admin`) → `Cajero` → `Auditor`. No distingue mayúsculas.
- Los demás roles (Farmacia, Médico, etc.) se ignoran. Si no queda ninguno de los tres, el acceso se niega.
- Si la respuesta es `200` con `token` pero sin el campo `exito`, se considera login correcto.
- Credenciales incorrectas: `401` o `200` con `"exito": false` y un `mensaje`.
- Pendiente de acordar: claims del JWT (`sub`, `role`, `exp`) y duración del token.

## 2. Disponibilidad

`GET {BaseUrl}/health` → `200 OK` si Integration está disponible. Caja usa esta respuesta para mostrar "En línea" / "Sin conexión".

## 3. Validación de subsidio (RF-CAJ-06)

Hoy Caja usa una implementación local (`MockApiService`) que consulta la tabla `Pacientes` de SQLite. Endpoint propuesto para reemplazarla:

`GET {BaseUrl}/pacientes/{cedula}/subsidio` — `cedula` en formato `000-0000000-0`. Lleva `Authorization: Bearer <token>`. Implementado en `ApiService` (se activa con `UseMockSubsidio: false`).

Nombres exactos que lee Caja (camelCase):

| Campo | Tipo | Regla acordada con el CORE (RN-02) |
|---|---|---|
| `encontrado` | bool | `true` si la cédula existe |
| `tieneSubsidio` | bool | `true` si el expediente está activo |
| `porcentaje` | decimal | `100` |
| `vigente` | bool | `true` si el expediente está activo. **Obligatorio:** sin `vigente: true` Caja cobra normalmente |
| `vigenciaHasta` | fecha o `null` | `null` (sin vencimiento) |
| `mensaje` | texto | `"Subsidio activo."`, `"Expediente inactivo."`, etc. |

Ejemplo con la regla del CORE:

```json
{
  "encontrado": true,
  "tieneSubsidio": true,
  "porcentaje": 100,
  "vigente": true,
  "vigenciaHasta": null,
  "mensaje": "Subsidio activo."
}
```

- Cédula inexistente: `404` o `200` con `"encontrado": false`.
- Cualquier otro error: Caja muestra el aviso y no aplica gratuidad.

**Importante (RN-01 / RN-03):** la respuesta no debe incluir diagnóstico, carga viral, CD4 ni ningún dato clínico.

## 4. Sincronización (modo sin conexión)

Todas las operaciones se guardan primero en SQLite con `Sincronizado = false`. Cuando `/health` responde, Caja envía los pendientes en este orden y marca como sincronizados los que Integration acepta:

| Orden | Endpoint | Cuerpo |
|---|---|---|
| 1 | `POST /sync/turnos` | `SyncTurnoCajaDto` |
| 2 | `POST /sync/movimientos` | `SyncMovimientoCajaDto` |
| 3 | `POST /sync/transacciones` | `SyncTransaccionDto` |
| 4 | `POST /sync/comprobantes` | `SyncComprobanteDto` |
| 5 | `POST /sync/fondos` | `SyncFondoConvenioDto` |

Respuesta esperada de cada envío (`200 OK`):

```json
{ "exito": true, "mensaje": "Registro recibido." }
```

Si `exito` es `false` o el código no es 2xx, el registro queda pendiente y se reintenta.

### Campos

**SyncTurnoCajaDto:** `idLocal`, `numeroTurno`, `usuarioCajero`, `nombreCajero`, `caja`, `fechaApertura`, `fechaCierre`, `balanceInicial`, `saldoEsperado`, `efectivoDeclarado`, `diferencia`, `estado` (`Abierto` / `Cerrado`), `observacion`.

**SyncMovimientoCajaDto:** `idLocal`, `turnoCajaIdLocal`, `numeroMovimiento`, `fecha`, `tipo` (`Entrada`, `Salida`, `Cobro`, `Subsidio`, `Ajuste`), `monto`, `concepto`, `referencia` (número de transacción), `usuarioCajero`.

**SyncTransaccionDto:** `idLocal`, `numeroTransaccion`, `fecha`, `cedulaPaciente`, `tipoServicio`, `monto`, `formaPago` (`Efectivo`, `Tarjeta`, `Subsidiado`), `esSubsidiado`, `estado` (`Pendiente`, `Completada`, `Anulada`), `comprobanteGenerado`, `usuarioCajero`, `numeroReceta`.

- `numeroReceta` (texto con el **id numérico** de la receta del CORE, por ejemplo `"15"`; puede ser `null`): obligatorio cuando `tipoServicio` es `Medicamentos ARV`; `null` en los demás servicios. Si el cajero escribe `REC-15`, Caja envía `15` (el CORE responde 400 a `"REC-15"`). El CORE lo usa para marcar la receta como descargada y habilitar el despacho en Farmacia. Mientras la terminal esté sin conexión, la receta sigue "pendiente de descargo" hasta que llegue la sincronización.

**SyncComprobanteDto:** `idLocal`, `transaccionIdLocal`, `numeroComprobante`, `fechaGeneracion`, `tipoDocumento` (`RECIBO DE CAJA` o `CONSTANCIA DE ENTREGA`), `esReimpresion`, `cantidadReimpresiones`. Se envían los metadatos; el PDF queda en la terminal.

**SyncFondoConvenioDto:** `idLocal`, `codigo`, `nombre`, `tipoFuente` (`PresupuestoEstatal`, `DonacionInternacional`), `organismo`, `montoAsignado`, `montoUtilizado`, `fechaInicio`, `fechaFin`, `activo`.

### Recomendaciones para Integration

- **Idempotencia:** usar los números de negocio como clave única (`numeroTurno`, `numeroMovimiento`, `numeroTransaccion`, `numeroComprobante`, `codigo`). Si llega un registro repetido, responder `exito: true` sin duplicarlo; así un reintento después de un corte de red no crea duplicados.
- Los `idLocal` son identificadores de la base SQLite de cada terminal: no deben usarse como clave en el Core.

## 5. Numeración de comprobantes

- Los comprobantes de ventanilla usan `<terminal>-<correlativo de 6 dígitos>`: `CJ01-000001`, `CJ01-000002`…
- Es una serie separada de los `REC-n` / `FAC-n` que emite el CORE para el Portal Web. No comparten numeración porque Caja emite comprobantes sin conexión.
- Cada terminal configura su prefijo en `Caja:Terminal` (solo letras y dígitos, máx. 10). Dos terminales no deben usar el mismo prefijo.
- `numeroComprobante` es único y sirve como clave de idempotencia en el CORE.
