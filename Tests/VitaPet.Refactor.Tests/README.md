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
