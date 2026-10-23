# Regresión de la refactorización de VitaPet Rework

Requiere Windows, .NET Framework 4.8, MSBuild de Visual Studio y SQL Server
local con autenticación integrada. No agrega dependencias.

```powershell
& 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe' Tests/VitaPet.Refactor.Tests/VitaPet.Refactor.Tests.csproj /restore /t:Build /m:1 /nr:false
& Tests/VitaPet.Refactor.Tests/bin/Debug/net48/VitaPet.Refactor.Tests.exe
```

Cada ejecución genera su propia base `VitaPet_Verificacion_Refactor_<guid>`
y la elimina en `finally`. Los archivos históricos quedan en una carpeta
temporal del mismo nombre. La configuración del sitio no se modifica.

Comprueba las tres lecturas de usuarios, login y estados de bloqueo, idioma,
rollback de Usuario/Bitácora/DVV, ausencia de escrituras de referencias sin
cambios, alta y modificación de mascotas y empresas, registro corporativo y
restablecimiento obligatorio, planes y coberturas, turnos y XML, rotación de
bitácora y detección de alteraciones.

Compara los parámetros, resultados, errores y datos de los procedimientos
modificados con sus versiones de las migraciones 026 y 027. Incluye versiones
desactualizadas, datos incompletos, fechas futuras, pertenencia, transacciones
y reemisión de facturas. También aplica nuevamente la migración 028 sobre una
base poblada y verifica que conserve sus referencias.

La suite existente `VitaPet.Facturacion.Tests` conserva las verificaciones de
pagos concurrentes, factura pendiente, pagos históricos y recorrido en Edge.
Las pruebas de este proyecto no realizan un backup/restore SQL real.

Para comprobar la cobertura de corrupción del checklist:

```powershell
& Tests/VitaPet.Refactor.Tests/bin/Debug/net48/VitaPet.Refactor.Tests.exe --integridad-todas
```

Esta variante prepara tres registros por cada una de las 20 tablas de datos,
altera un campo sin recalcular sus verificadores y exige que el diagnóstico
identifique cada fila y columna. También prueba nueve alteraciones simultáneas,
los cuatro controles de integridad y una tabla nueva sin cobertura. Cada caso
revierte sus cambios y comprueba que la base vuelva a estar íntegra. Usa y
elimina su propia base; no toca la base VitaPet ni la base del checklist manual.

Las 20 tablas de datos tienen DVH por fila y un DVV por tabla. Los cuatro
controles son `DVV`, `ReferenciaIntegridad`, `__FormatoIntegridad` y
`__SchemaMigrations`; se comprueban mediante referencias, formato y checksums
de migraciones. La suite también exige que el inventario SQL coincida con esas
24 tablas. `Database/VerificarCoberturaIntegridad.sql` permite consultar la
cobertura y el formato de los hashes sin modificar la base seleccionada;
la comparación de hashes con los datos se realiza desde la aplicación.

La variante `--integridad-login` también registra y consulta los errores de
cada tabla en el histórico XML, verifica que no se alteren datos ni controles
SQL y comprueba el acceso del Webmaster con cuenta de emergencia configurada.
Prueba además el rechazo de un histórico adulterado y de usuarios sin permiso.
