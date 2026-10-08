# Documentación técnica — Módulo Caja (CONAVIHSIDA)

Aplicación de escritorio para la ventanilla de Caja: apertura y cierre de turno, cobros, entregas subsidiadas, comprobantes PDF, fondos y convenios, historial, trabajo sin conexión y bitácora de auditoría.

## 1. Tecnología

| Área | Elección |
|---|---|
| Lenguaje y plataforma | C# · .NET 8 |
| Interfaz | Avalonia UI 12 (tema Fluent, siempre en modo claro) · patrón MVVM con CommunityToolkit.Mvvm |
| Datos locales | SQLite con Entity Framework Core 8 (`cajaapp.db` junto al ejecutable) |
| Comunicación | HttpClient + System.Text.Json hacia Integration |
| Comprobantes | QuestPDF (licencia Community) |
| Bitácora | Serilog → `Logs/caja-AAAAMMDD.log` (30 días) |
| Pruebas | xUnit (`CajaApp.Tests`) |
| IDE | Visual Studio 2022 |

## 2. Arquitectura

```
Vista (.axaml)  →  ViewModel  →  Servicio (Services/)  →  AppDbContext (SQLite)
                                       ↓
                               SyncApiService → Integration (HTTP)
```

- **Views** solo describen la interfaz y se enlazan por *binding*; no contienen lógica.
- **ViewModels** validan lo que escribe el usuario, llaman a los servicios y registran en la bitácora.
- **Services** contienen las reglas de negocio y el acceso a datos. Se registran en `App.axaml.cs` con inyección de dependencias.
- **ViewLocator** asocia cada `XxxViewModel` con su `XxxView` automáticamente.
- **INavegacionCaja** permite que una pantalla pida ir a otra (por ejemplo, de Validar subsidio a Nueva transacción con la cédula ya cargada) sin depender del Shell.

### Pantallas

| Pantalla | ViewModel | Requisito |
|---|---|---|
| Login | `LoginViewModel` | Autenticación, roles Cajero / Administrador / Auditor |
| Inicio | `InicioViewModel` | Resumen del turno, acciones rápidas, últimos movimientos, pendientes |
| Apertura de caja | `AperturaViewModel` | RF-CAJ-01 |
| Nueva transacción | `TransaccionViewModel` | RF-CAJ-03, RF-CAJ-04, RF-CAJ-06 |
| Validar subsidio | `SubsidioViewModel` | RF-CAJ-06, RN-03 |
| Fondos y convenios | `FondosViewModel` | RF-CAJ-05 |
| Historial | `HistorialViewModel` | Trazabilidad y reimpresión |
| Cuadre y cierre | `CierreViewModel` | RF-CAJ-01, RF-CAJ-02 |
| Preguntas frecuentes | `AyudaViewModel` | Ayuda de uso (búsqueda y filtro por tema) |

## 3. Reglas de negocio implementadas

| Regla | Dónde |
|---|---|
| Solo un turno abierto a la vez; no se cobra sin turno abierto | `CajaService.AbrirCajaAsync`, `TransaccionService.RegistrarAsync` |
| Efectivo esperado = balance inicial + cobros en efectivo (tarjeta y subsidios no cuentan) | `TransaccionService`, `CajaService.CerrarCajaAsync` |
| Diferencia = efectivo contado − efectivo esperado; si no es cero, se exige observación | `CierreViewModel` |
| Entrega subsidiada = RD$ 0.00, sin movimiento de efectivo (RN-02) | `TransaccionService` + restricción CHECK |
| La gratuidad solo aplica a la cédula validada; si la cédula cambia, se invalida | `TransaccionViewModel` |
| Toda operación termina con comprobante y queda "Completada" (RN-05) | `ComprobanteService.GenerarAsync` |
| Caja no ve ni guarda datos clínicos; la cédula se muestra enmascarada (RN-01, RN-03) | `CedulaHelper`, vistas y bitácora |
| Cédula de 11 dígitos, normalizada a `000-0000000-0` | `CedulaHelper` |
| Montos con tope de RD$ 1,000,000 por operación | `Formato.MontoMaximoOperacion` |

## 4. Base de datos e integridad (RF-CAJ-07)

Tablas: `Pacientes`, `TurnosCaja`, `MovimientosCaja`, `Transacciones`, `Comprobantes`, `FondosConvenios`.

Restricciones **CHECK** definidas en `AppDbContext` (sin triggers):

- Montos, balances y efectivo declarado no negativos.
- `FormaPago`, `Estado`, `Tipo` y `TipoFuente` solo aceptan valores válidos.
- Una transacción subsidiada debe tener monto 0.
- `CantidadReimpresiones` ≥ 0.

Como SQLite guarda los decimales como texto, las restricciones usan `CAST(... AS REAL)`.

**Nota:** la base se crea con `EnsureCreated()`. Las restricciones nuevas se aplican cuando la base se crea desde cero. Para tenerlas en una base existente, cierra la app y borra `bin/Debug/net8.0/cajaapp.db` (se vuelve a crear con los datos de prueba).

## 5. Trabajo sin conexión

1. Cada operación se guarda en SQLite con `Sincronizado = false`.
2. `BackgroundSyncService` consulta `/health` cada 30 segundos.
3. Si Integration responde, `SyncService` envía los pendientes (turnos → movimientos → transacciones → comprobantes → fondos) y marca los aceptados.
4. Si no responde, Caja sigue operando; la interfaz muestra "Sin conexión · N pendientes".
5. El botón **Sincronizar ahora** de la barra lateral fuerza un intento.

Los envíos llevan `Authorization: Bearer <token>` con el token del login. Detalle de los endpoints en `CONTRATO_API.md`.

## 6. Bitácora de auditoría

`AuditService` escribe una línea por evento en `Logs/caja-AAAAMMDD.log`:

```
2026-10-07 20:22:32.259 [INF] [TRANSACCION] Transacción TX-20261007-202232080 registrada. Servicio: Consulta médica. Pago: Subsidiado. Monto: RD$ 0.00. Beneficiario: 001-XXXX567-8. | Usuario: cajero | Terminal: CAJA-01
```

Eventos: `SISTEMA`, `LOGIN`, `LOGOUT`, `APERTURA_CAJA`, `TRANSACCION`, `SUBSIDIO`, `COMPROBANTE`, `REIMPRESION`, `CIERRE_CAJA`, `FONDO`, `SYNC`, `ERROR`. Los errores no controlados también quedan registrados y no cierran la aplicación.

## 7. Interfaz

- Estilos globales en `Styles/Tema.axaml` y colores, fuentes e íconos en `Styles/Recursos.axaml`. Para cambiar un color en toda la app basta con editar `Recursos.axaml`.
- Clases reutilizables: `Border.card`, `Border.stat`, `Border.notice` (`.verde`, `.rojo`, `.amarillo`), `Border.badge`, `Button.primary`, `.secondary`, `.ghost`, `.danger`, `TextBlock.h1`, `.label`, `.amount`, etc.
- Tipografías del prototipo incluidas en `Assets/Fonts`: Public Sans (texto) e IBM Plex Mono (cifras). Ambas con licencia SIL Open Font License.
- `App.axaml` fija `RequestedThemeVariant="Light"` para que los campos no se vuelvan negros en equipos con Windows en modo oscuro.

## 8. Configuración (`appsettings.json`)

| Clave | Uso |
|---|---|
| `ApiSettings:BaseUrl` | URL base de Integration |
| `ApiSettings:UseMockAuthentication` | `true` = login de demostración (`cajero` / `1234`); `false` = login real contra Integration |

## 9. Pendientes conocidos

- Reemplazar `MockApiService` por la consulta real de subsidio cuando Integration publique el endpoint.
- Diferenciar vistas por rol (por ejemplo, que Administrador y Auditor vean los turnos de todos los cajeros).
- Pasar de `EnsureCreated()` a migraciones de EF Core si el esquema sigue cambiando.
- Impresión térmica ESC/POS (por ahora el comprobante es PDF).
