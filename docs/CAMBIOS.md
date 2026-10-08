# Cambios del rediseño (7 de octubre de 2026)

Copia de seguridad del estado anterior: `D:\Desarrollo3Caja\_respaldo_antes_rediseno`.

## Interfaz

- **Campos negros al seleccionarlos: corregido.** La app usaba el tema del sistema (`RequestedThemeVariant="Default"`); en Windows con modo oscuro, el campo enfocado tomaba fondo negro. Ahora el tema es siempre claro y los campos tienen estilo propio para todos sus estados (normal, encima, enfocado, deshabilitado).
- Sistema de estilos global (`Styles/Tema.axaml`, `Styles/Recursos.axaml`) con la paleta, tarjetas, avisos, insignias y botones del prototipo.
- Tipografías del prototipo incluidas: Public Sans e IBM Plex Mono.
- **Login:** logo completo arriba de la tarjeta, etiqueta "Módulo Caja", botón a todo el ancho (Enter también inicia sesión), aviso de error, ayuda del modo demostración y franja tricolor.
- **Barra lateral:** ícono de CONAVIHSIDA arriba a la izquierda; al hacer clic vuelve a Inicio y actualiza los datos. Estado de la caja con turno y balance, menú por secciones con la pantalla activa resaltada, estado de conexión y botón Sincronizar ahora.
- **Barra superior:** título de la pantalla, ruta, estado de conexión, usuario con iniciales y botón Salir.
- **Inicio más completo:** saludo, aviso si la caja está cerrada, 4 indicadores del turno (estado, total cobrado, entregas subsidiadas, efectivo esperado), acciones rápidas, aviso de pendientes y últimos movimientos.
- Apertura, Nueva transacción, Validar subsidio, Fondos, Historial y Cuadre/cierre rediseñadas según el prototipo (formulario + tarjeta lateral, vista previa del comprobante, tablas con insignias, cuadre con aviso verde/amarillo/rojo).
- El comprobante PDF ahora lleva el logo institucional.
- Ícono propio para la ventana y el ejecutable.

## Validaciones y seguridad

- Cédula validada (11 dígitos) y normalizada a `000-0000000-0`; se acepta escribirla sin guiones.
- Se quitó la casilla que permitía marcar una operación como subsidiada sin validar. La gratuidad solo se aplica después de validar el subsidio, y solo a esa cédula: si se cambia, la validación se reinicia.
- Montos leídos en varios formatos (`5000`, `5,000.50`, `RD$ 1,250`) con tope por operación.
- El cierre exige escribir el efectivo contado y, si hay diferencia, una observación.
- Restricciones CHECK en la base de datos para montos, estados y formas de pago (RF-CAJ-07).
- La contraseña se borra de memoria después del login.
- Los envíos de sincronización llevan el token del login (`Authorization: Bearer`).
- La base de datos se ubica junto al ejecutable, sin depender de la carpeta desde la que se abra la app.

## Auditoría

- Nuevo `AuditService` (Serilog) con archivo diario en `Logs/` y retención de 30 días.
- Se registran login (correcto y rechazado), logout, apertura, consultas de subsidio, transacciones, comprobantes, reimpresiones, cierres, fondos, cambios de conexión y sincronizaciones.
- Los errores no controlados se registran y ya no cierran la aplicación.

## Pruebas y documentación

- Proyecto `CajaApp.Tests` con 40 pruebas xUnit (todas correctas).
- Carpeta `docs/` con documentación técnica, contrato de API para Integration y plan de pruebas de aceptación.

## Ajustes posteriores

- **Tamaño de ventana:** la app se abre maximizada cuando la pantalla es menor de 1368 × 888 (por ejemplo, laptops de 1366 × 768), respetando la barra de tareas y la escala de Windows. En pantallas grandes abre a 1320 × 840, centrada. El tamaño mínimo bajó a 960 × 580.
- **Login más compacto:** la tarjeta cabe en pantallas bajas y, si aun así no cabe, se puede desplazar.
- **Nueva sección "Preguntas frecuentes"** (barra lateral, apartado Ayuda): 24 preguntas en 7 temas, con búsqueda (sin importar tildes) y filtro por tema.
- Se quitaron los paneles explicativos de Apertura, Validar subsidio y Nueva transacción; su contenido ahora está en Preguntas frecuentes.
