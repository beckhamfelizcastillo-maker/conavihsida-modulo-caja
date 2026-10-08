# CajaApp — Módulo Caja de CONAVIHSIDA

Aplicación de escritorio (C#, .NET 8, Avalonia UI) para la ventanilla de Caja del Sistema Integral de Gestión de CONAVIHSIDA.

## Abrir y ejecutar

1. Abrir `CajaApp.sln` en **Visual Studio 2022**.
2. Seleccionar `CajaApp` como proyecto de inicio y presionar **F5**.
3. Iniciar sesión con el usuario de demostración: **cajero** / **1234**.

Cédulas de prueba: `001-1234567-8` (subsidio activo) y `001-9999999-9` (subsidio vencido).

## Pruebas

Menú **Prueba → Ejecutar todas las pruebas**. La primera vez Visual Studio descarga xUnit.

## Estructura

```
CajaApp/
├── Assets/          Logos, fondo del login, ícono y fuentes (Public Sans, IBM Plex Mono)
├── Data/            AppDbContext (SQLite) con restricciones CHECK y datos de prueba
├── DTOs/            Contratos JSON con Integration
├── Helpers/         Formato de montos y reglas de la cédula
├── Models/          Turno, Transacción, Movimiento, Comprobante, Fondo, Paciente
├── Services/        Reglas de negocio, comprobantes PDF, sincronización y bitácora
├── Styles/          Tema visual global (colores, tipografía, botones, tarjetas)
├── ViewModels/      Lógica de cada pantalla (MVVM)
└── Views/           Pantallas (.axaml)
CajaApp.Tests/       Pruebas automáticas (xUnit)
docs/                Documentación técnica, contrato de API y plan de pruebas
```

## Archivos generados al ejecutar (en `bin/Debug/net8.0`)

- `cajaapp.db` — base de datos local.
- `Comprobantes/` — recibos PDF.
- `Logs/` — bitácora de auditoría diaria.

## Documentación

- `docs/DOCUMENTACION_TECNICA.md` — arquitectura, reglas de negocio, base de datos, modo sin conexión y bitácora.
- `docs/CONTRATO_API.md` — endpoints que Integration debe exponer.
- `docs/PLAN_DE_PRUEBAS.md` — pruebas automáticas y guion de pruebas de aceptación.
- `docs/CAMBIOS.md` — cambios del rediseño.
