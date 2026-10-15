# CU-001-001 Registro de cliente

Compilar `VitaPet.RegistroCliente.Tests.csproj` con MSBuild y ejecutar el EXE
de `bin/Debug/net48`. Usa una base `VitaPet_Verificacion_Registro_<guid>`
que se elimina al terminar; no modifica la base VitaPet.

Comprueba validación del servidor, alta pública con rol fijo, hash compatible
con Login, relación Usuario–Cliente, DVH/DVV/referencias, correo duplicado,
concurrencia, rollback de alta y bitácora, y rechazo por integridad alterada.

Después de compilar, `Run-Web.ps1` prueba el recorrido desde Login en una
copia del sitio con IIS Express: errores, alta, recarga, inicio de sesión,
formulario móvil y validación sin JavaScript. Guarda capturas en
`artifacts/registro-web` y elimina la base de pruebas al terminar.
