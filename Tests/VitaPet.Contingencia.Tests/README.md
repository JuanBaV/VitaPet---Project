# Verificación de recuperación del WebMaster

Requiere Windows, .NET Framework 4.8, MSBuild y SQL Server local (instancia por defecto, autenticación integrada). El usuario de pruebas debe poder crear, respaldar, restaurar y eliminar bases temporales. El ejecutable concede Modify a `NT SERVICE\MSSQLSERVER` sólo en su carpeta temporal de backups.

```powershell
MSBuild.exe Tests/VitaPet.Contingencia.Tests/VitaPet.Contingencia.Tests.csproj /restore /t:Build
& Tests/VitaPet.Contingencia.Tests/bin/Debug/net48/VitaPet.Contingencia.Tests.exe
```

Cada ejecución utiliza una base `VitaPet_Verificacion_Contingencia_<guid>` y archivos temporales propios. La base se elimina en `finally`. Nunca apuntar estas pruebas a VitaPet ni a datos reales.

Comprueba permisos de configuración, cuenta portable con verificador PBKDF2, rechazo de formatos desconocidos, copia de la cuenta y cambio de servidor de origen, desactivación en estado sano, respaldo automático al generar un backup, cuenta eliminada, tablas y base eliminadas, XML manipulado, XML firmado con DVH inválido, restricciones de permisos, expiración, bloqueo de intentos persistente, restauración del histórico, fallas de archivo durante restauración y reintento con mantenimiento activo.

Para el flujo real en navegador:

```powershell
& Tests/VitaPet.Contingencia.Tests/Run-WebChecks.ps1
```

El script usa una copia temporal del sitio en IIS Express (puerto 60319), la base aislada y Playwright con Edge. Ajustar las rutas de MSBuild y Node al entorno si hace falta. Cierra únicamente su proceso IIS y elimina su propia base al terminar. Verifica configuración, login, restricciones GET/POST, acceso en mantenimiento, confirmación/cancelación y login del WebMaster recuperado. Las capturas quedan en `artifacts/contingencia-web`.

La suite también valida las trece tablas funcionales, alteraciones por campo, ausencia de DVV, nulos, decimales independientes de la región y migración/restauración del esquema previo.

También comprueba la restauración de backups SQL y XML anteriores a la actualización 013, y verifica que se reinstalen los procedimientos vigentes sin alterar la integridad.
