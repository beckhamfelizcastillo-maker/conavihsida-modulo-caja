# CajaApp — Módulo Caja de CONAVIHSIDA

Aplicación de escritorio (C#, .NET 8, Avalonia UI) para la ventanilla de Caja del Sistema Integral de Gestión de CONAVIHSIDA. Administra turnos, cobros, entregas subsidiadas de ARV, comprobantes, fondos y el cuadre de caja. Sigue funcionando sin conexión y sincroniza con el CORE a través del Gateway de Integración.

## Requisitos

- **Visual Studio 2022** actualizado (17.12 o superior) o el **SDK de .NET 10**. El proyecto apunta a .NET 8, pero Avalonia 12 necesita un compilador reciente: con un SDK viejo aparecen errores de `InitializeComponent`.
- Para la operación en línea: el **CORE** (Emilia) y el **Gateway de Integración** (Ricardo) corriendo en la misma computadora.

## Abrir y ejecutar

1. Abrir `CajaApp.sln` en Visual Studio.
2. Seleccionar `CajaApp` como proyecto de inicio y presionar **F5**.

### Modo conectado (prueba conjunta)

1. Arrancar el CORE y luego el Gateway. Comprobar que `https://localhost:7107/swagger` abre en el navegador.
2. `CajaApp/appsettings.json` debe quedar así (es la configuración actual del repositorio):

   ```json
   {
     "ApiSettings": {
       "BaseUrl": "https://localhost:7107/api",
       "UseMockAuthentication": false,
       "UseMockSubsidio": false
     },
     "Caja": { "Terminal": "CJ01" }
   }
   ```

   Si hay problemas con el certificado HTTPS, usar `"BaseUrl": "http://localhost:5223/api"`.

3. Iniciar sesión con **cajero.prueba** / **Prueba.Core.2026**.

Cédulas de prueba del CORE:

| Cédula | Resultado esperado |
| --- | --- |
| `000-0000004-0` | Subsidio activo |
| `000-0000005-0` | Expediente inactivo: se cobra |
| `000-0000099-0` | No existe |

### Modo demostración (sin Gateway)

Para ver Caja sin el CORE ni el Gateway, poner en `appsettings.json`:

```json
"UseMockAuthentication": true,
"UseMockSubsidio": true
```

Entrar con **cajero** / **1234**. Cédulas locales: `001-1234567-8` (subsidio activo) y `001-9999999-9` (subsidio vencido). Volver a poner ambos valores en `false` antes de subir cambios o de la prueba conjunta.

### Sin conexión (operación continua)

Si el Gateway o el CORE dejan de responder, Caja sigue registrando transacciones, emitiendo comprobantes y haciendo el cuadre. Todo queda pendiente y se sincroniza solo cuando vuelve la conexión.

- **Login sin conexión:** solo pueden entrar usuarios que ya iniciaron sesión **en línea** en esa computadora en los últimos 7 días. Para la demostración, entrar una vez en línea antes de apagar el CORE.
- **Subsidio sin conexión:** se usa el último resultado guardado de esa cédula. Si la cédula nunca se validó en esa terminal, se cobra normalmente.
- **Sesión vencida:** el token del Gateway dura 8 horas. Si vence, la barra lateral lo avisa: cerrar sesión y volver a entrar para enviar los pendientes.

## Reglas de negocio principales

- **Gratuidad solo para ARV (RN-02, RF-CAJ-04):** *Medicamentos ARV* con subsidio activo se entrega a RD$ 0.00. Consulta médica, laboratorio y procedimiento son servicios arancelados y se cobran aunque el paciente tenga subsidio.
- **Número de receta:** obligatorio en *Medicamentos ARV*. Es el id numérico de la receta del CORE (si dice `REC-15`, se guarda y envía `15`). Con él, Farmacia sabe que la receta ya pasó por Caja.
- **Comprobante obligatorio (RN-05, RF-CAJ-03):** toda operación emite su PDF (`CJ01-000001`, `CJ01-000002`…), también sin conexión. Es una serie distinta de las facturas `REC-n` / `FAC-n` del Portal Web.
- **Cuadre de caja (RF-CAJ-01, RF-CAJ-02):** al cerrar el turno se compara el efectivo esperado con el contado y se genera el **reporte de cuadre en PDF** (exacto, sobrante o faltante, con observación y firmas).
- **Roles:** el login acepta la lista de roles del CORE y entra con uno: Administrador, luego Cajero, luego Auditor.
- **Confidencialidad (RN-01, RN-03):** Caja no ve ni guarda datos clínicos; la cédula se muestra enmascarada.

## Pruebas

Menú **Prueba → Ejecutar todas las pruebas** (76 pruebas xUnit). La primera vez Visual Studio descarga xUnit. Las pruebas usan una base SQLite temporal y no tocan `cajaapp.db`.

## Estructura

```
CajaApp/
├── Assets/          Logos, fondo del login, ícono y fuentes (Public Sans, IBM Plex Mono)
├── Data/            AppDbContext (SQLite) con restricciones CHECK y datos de prueba
├── DTOs/            Contratos JSON con Integración
├── Helpers/         Formato de montos, cédula y roles
├── Models/          Turno, Transacción, Movimiento, Comprobante, Fondo, Paciente
├── Services/        Reglas de negocio, comprobantes y reporte de cuadre en PDF,
│                    login y subsidio (en línea y sin conexión), sincronización y bitácora
├── Styles/          Tema visual global (colores, tipografía, botones, tarjetas)
├── ViewModels/      Lógica de cada pantalla (MVVM)
└── Views/           Pantallas (.axaml)
CajaApp.Tests/       Pruebas automáticas (xUnit)
docs/                Documentación técnica, contrato de API, plan de pruebas y cambios
```

## Archivos generados al ejecutar (en `bin/Debug/net8.0`)

- `cajaapp.db`: base de datos local (cola de sincronización). Las columnas nuevas se agregan solas al abrir la app.
- `Comprobantes/`: recibos y constancias en PDF.
- `Reportes/`: reportes de cuadre en PDF.
- `Logs/`: bitácora de auditoría diaria (30 días).
- `sesion-offline.json`: usuarios habilitados para el login sin conexión (solo hash de la contraseña, nunca la contraseña).

## Documentación

- `docs/DOCUMENTACION_TECNICA.md`: arquitectura, reglas de negocio, base de datos, modo sin conexión y bitácora.
- `docs/CONTRATO_API.md`: endpoints del Gateway que usa Caja (login, health, subsidio y sincronización), con los nombres exactos de cada campo.
- `docs/PLAN_DE_PRUEBAS.md`: pruebas automáticas y guion de pruebas de aceptación.
- `docs/CAMBIOS.md`: historial de cambios.
