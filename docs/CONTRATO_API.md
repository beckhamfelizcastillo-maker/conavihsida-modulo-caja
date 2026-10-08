# Contrato de API — Caja ↔ Integration

Documento para el equipo de **Integration** (Ricardo). Describe los endpoints que el módulo **Caja** ya consume. La URL base se configura en `CajaApp/appsettings.json`:

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5001/api",
    "UseMockAuthentication": true
  }
}
```

- Mientras `UseMockAuthentication` sea `true`, el login no llama a Integration (usuario de demostración `cajero` / `1234`).
- Para conectar con Integration real: poner la URL correcta en `BaseUrl` y cambiar `UseMockAuthentication` a `false`.

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
  "rol": "Cajero",
  "nombreCompleto": "María Rosario",
  "mensaje": "Autenticación correcta."
}
```

- Roles aceptados por Caja: `Cajero`, `Administrador`, `Auditor`. Cualquier otro rol es rechazado.
- Credenciales incorrectas: `401` o `200` con `"exito": false` y un `mensaje`.
- Pendiente de acordar: claims del JWT (`sub`, `role`, `exp`) y duración del token.

## 2. Disponibilidad

`GET {BaseUrl}/health` → `200 OK` si Integration está disponible. Caja usa esta respuesta para mostrar "En línea" / "Sin conexión".

## 3. Validación de subsidio (RF-CAJ-06)

Hoy Caja usa una implementación local (`MockApiService`) que consulta la tabla `Pacientes` de SQLite. Endpoint propuesto para reemplazarla:

`GET {BaseUrl}/pacientes/{cedula}/subsidio` — `cedula` en formato `000-0000000-0`.

```json
{
  "encontrado": true,
  "tieneSubsidio": true,
  "porcentaje": 100,
  "vigente": true,
  "vigenciaHasta": "2027-12-31T00:00:00",
  "mensaje": "Subsidio activo."
}
```

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

**SyncTransaccionDto:** `idLocal`, `numeroTransaccion`, `fecha`, `cedulaPaciente`, `tipoServicio`, `monto`, `formaPago` (`Efectivo`, `Tarjeta`, `Subsidiado`), `esSubsidiado`, `estado` (`Pendiente`, `Completada`, `Anulada`), `comprobanteGenerado`, `usuarioCajero`.

**SyncComprobanteDto:** `idLocal`, `transaccionIdLocal`, `numeroComprobante`, `fechaGeneracion`, `tipoDocumento` (`RECIBO`), `esReimpresion`, `cantidadReimpresiones`. Se envían los metadatos; el PDF queda en la terminal.

**SyncFondoConvenioDto:** `idLocal`, `codigo`, `nombre`, `tipoFuente` (`PresupuestoEstatal`, `DonacionInternacional`), `organismo`, `montoAsignado`, `montoUtilizado`, `fechaInicio`, `fechaFin`, `activo`.

### Recomendaciones para Integration

- **Idempotencia:** usar los números de negocio como clave única (`numeroTurno`, `numeroMovimiento`, `numeroTransaccion`, `numeroComprobante`, `codigo`). Si llega un registro repetido, responder `exito: true` sin duplicarlo; así un reintento después de un corte de red no crea duplicados.
- Los `idLocal` son identificadores de la base SQLite de cada terminal: no deben usarse como clave en el Core.
