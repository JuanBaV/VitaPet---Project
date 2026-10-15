# Verificación de CU-007-004

Requiere .NET Framework 4.8, MSBuild y SQL Server local con autenticación integrada.

```powershell
MSBuild.exe Tests/VitaPet.Usuarios.Tests/VitaPet.Usuarios.Tests.csproj /restore /t:Build
& Tests/VitaPet.Usuarios.Tests/bin/Debug/net48/VitaPet.Usuarios.Tests.exe
```

Cada ejecución crea una base `VitaPet_Verificacion_Usuarios_<guid>`, aplica las
migraciones, prepara usuarios con todas las combinaciones de estado y bloqueo y
elimina exclusivamente esa base al finalizar. No usa la base VitaPet.

Comprueba búsqueda parcial, mayúsculas y acentos, filtros individuales/combinados,
caracteres SQL literales, permisos, ausencia de hashes en el listado, selección,
auditoría de consultas vacías, rotación del histórico, integridad y ausencia de
modificaciones de usuarios. Verifica también la reasignación ER028/ER035.

El modo `--web` conserva una base de pruebas y escribe `web-environment.txt` junto
al ejecutable para pruebas de navegador. Debe eliminarse esa base al terminar.

## Idioma de la interfaz

`Idioma.cs` comprueba persistencia del idioma, permisos, DVH/DVV, referencias y
rollback de sesión y datos ante un fallo SQL.

Ejecutar `Run-IdiomaWeb.ps1` para comprobar el selector desplegable, navegación,
recursos en ES/EN, mensajes dinámicos, valores invariantes de filtros y roles,
validación cliente/servidor y adaptación a móvil en Edge. Usa una copia del sitio
y una base aislada; elimina su base y cierra su IIS Express al finalizar.
Las capturas quedan en `artifacts/idioma-web`.

## Extensiones CU-007-005 a CU-007-008

`Extensiones.cs` verifica detalle auditado, modificación de los cuatro campos,
conservación de contraseña/idioma/bloqueo/intentos, detección de edición
concurrente, validaciones, bloqueo y desbloqueo con relectura del estado,
permisos y catálogo ER036–ER040. Inyecta fallas mediante triggers y bloqueo del
archivo histórico para comprobar rollback de Usuario, Bitácora, DV y referencias.
Las pruebas se ejecutan dentro del mismo programa, sin usuarios reales.

`browser-check.cjs` prueba las pantallas con una copia aislada del sitio. Requiere
Playwright y Edge, más un JSON con `Url` apuntando a esa copia. Acepta la ruta del
JSON como primer argumento y el directorio de capturas como segundo argumento.
Usa los usuarios ficticios preparados por `--web`; no apuntar a la base VitaPet.
Comprueba navegación, validaciones, cancelación, dos formularios simultáneos,
confirmaciones, tickets consumidos, vista móvil y cambios de correo/rol propios.
Al finalizar hay que detener el IIS de pruebas y eliminar su base temporal.

Las cuatro operaciones se inician desde los botones de `GestionUsuarios.aspx`.
El identificador y la versión de edición se conservan en sesión del servidor.
Cada confirmación revalida permisos, integridad y datos vigentes. La migración
011 se aplica automáticamente al iniciar la aplicación. Si se modifica el propio
Webmaster, se actualiza su sesión después del commit; si pierde el rol o queda
bloqueado/inactivo, se informa el resultado sin devolver acceso a Gestión.

Procedimientos.cs compara los 21 contratos de parámetros y contrasta los procedimientos simplificados con sus versiones anteriores para filtros, valores nulos, concurrencia, duplicados y errores. Cada actualización comparada se revierte en una transacción de la base aislada.
