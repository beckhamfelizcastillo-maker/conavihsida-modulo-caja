# Plan de pruebas — Módulo Caja

## 1. Pruebas automáticas (xUnit)

Proyecto: `CajaApp.Tests` (incluido en la solución).

**Cómo ejecutarlas en Visual Studio 2022:** menú **Prueba → Ejecutar todas las pruebas** (o `Ctrl+R, A`). La primera vez Visual Studio descarga los paquetes de xUnit automáticamente.

Las pruebas usan una base SQLite temporal propia, así que no tocan `cajaapp.db`.

| Archivo | Qué comprueba |
|---|---|
| `FormatoTests` | Lectura de montos escritos de varias formas (`5000`, `5,000.50`, `RD$ 1,250`, `300,5`) y formato `RD$ 1,234.50`. |
| `CedulaHelperTests` | Cédula de 11 dígitos, normalización a `000-0000000-0` y enmascarado `001-XXXX567-8`. |
| `CajaServiceTests` | Apertura con balance inicial, bloqueo de un segundo turno abierto, balance negativo rechazado y cálculo de diferencia al cierre (solo cuenta el efectivo). |
| `TransaccionServiceTests` | Sin turno no se cobra; entrega subsidiada = RD$ 0.00 sin movimiento de efectivo; efectivo suma al saldo y tarjeta no; cédula y monto inválidos rechazados; restricciones CHECK de la base (monto negativo, subsidiada con monto). |
| `SubsidioYComprobanteTests` | Subsidio activo, vencido y no registrado; cédula sin guiones; el comprobante crea el PDF y marca la transacción como completada. |

Resultado verificado: **40 de 40 pruebas correctas**.

## 2. Pruebas de aceptación (manuales, para la demostración)

Usuario de demostración: `cajero` / `1234`. Cédulas de prueba: `001-1234567-8` (subsidio activo), `001-9999999-9` (subsidio vencido).

| # | Prueba | Pasos | Resultado esperado |
|---|---|---|---|
| 1 | Login correcto | Usuario `cajero`, contraseña `1234`, Enter | Entra a Inicio. Bitácora: `[LOGIN] Inicio de sesión correcto`. |
| 2 | Login incorrecto | Contraseña `999` | Aviso rojo "Usuario o contraseña incorrectos". Bitácora: `[WRN] [LOGIN]`. |
| 3 | Campos del login | Hacer clic en cada campo | El campo queda blanco con borde azul (nunca negro, aunque Windows esté en modo oscuro). |
| 4 | Apertura | Apertura de caja → balance `5,000` → Abrir caja | Confirmación verde con número de turno; la barra lateral pasa a "Caja abierta". |
| 5 | Doble apertura | Volver a Apertura con un turno abierto | Aviso "Ya tienes un turno abierto"; no permite abrir otro. |
| 6 | Cobro en efectivo | Nueva transacción → cédula `402-1234567-1` → Consulta médica `300` → Efectivo → Registrar | Aviso verde con número de comprobante; botón "Ver comprobante PDF" abre el recibo. El efectivo esperado sube RD$ 300. |
| 7 | Cobro con tarjeta | Igual que 6 con forma de pago Tarjeta | Se registra; el efectivo esperado **no** cambia. |
| 8 | Entrega subsidiada | Cédula `00112345678` → Validar subsidio | La cédula se normaliza, aparece "Subsidio activo", el total pasa a RD$ 0.00 y el monto/forma de pago se bloquean. Registrar emite "Constancia de entrega subsidiada". |
| 9 | Cambio de cédula después de validar | Validar `001-1234567-8` y luego cambiar un dígito | La validación se reinicia y el total vuelve a cobrarse (evita aplicar la gratuidad a otra persona). |
| 10 | Subsidio vencido | Validar subsidio → `001-9999999-9` | Insignia "Vencido"; la cobertura indica que se cobra el servicio. |
| 11 | Datos inválidos | Cédula `123`, monto `abc` o `0` | Mensajes claros; no se registra nada. |
| 12 | Historial | Historial → buscar `567-8` → PDF | Filtra por cédula; abre el comprobante y registra la reimpresión. |
| 13 | Fondos | Registrar nueva partida con código repetido o monto 0 | Se rechaza con mensaje; con datos válidos aparece en la tabla y en los totales. |
| 14 | Cuadre con diferencia | Cuadre y cierre → contado menor al esperado → Cerrar | Aviso rojo de faltante; exige una observación de al menos 10 caracteres. |
| 15 | Cierre | Escribir la observación → Cerrar caja | Pantalla "Turno cerrado correctamente"; la barra lateral vuelve a "Caja sin abrir" y Nueva transacción queda bloqueada. |
| 16 | Modo sin conexión | Con Integration apagado, registrar operaciones | Insignia "Sin conexión · N pendientes"; el contador sube y Caja sigue operando. |
| 17 | Logotipo | Clic en el logo de la barra lateral | Vuelve a Inicio y actualiza turno, indicadores y sincronización. |
| 18 | Bitácora | Abrir `bin/Debug/net8.0/Logs/caja-AAAAMMDD.log` | Cada evento tiene fecha, usuario, terminal y evento; las cédulas aparecen enmascaradas y no hay datos clínicos. |
