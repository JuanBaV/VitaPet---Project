# Recuperación del usuario WebMaster

## Preparación

1. Iniciar sesión con un WebMaster activo y confiable y abrir **Base de datos → Recuperación del WebMaster**.
2. Definir un correo de emergencia y una contraseña de 12 a 100 caracteres con mayúscula, número y símbolo. No se incluyen credenciales universales ni contraseñas en el repositorio.
3. Seleccionar **Guardar cuenta y respaldo inicial**. Se genera un XML firmado con un estado sano antes de guardar la cuenta. Se cierran las sesiones existentes; volver a ingresar normalmente.
4. Guardar las credenciales fuera de la aplicación. Crear nuevos puntos mediante **Crear respaldo XML de contingencia**. El botón existente **Crear backup** también genera un XML cuando la cuenta está configurada.

La cuenta se prepara antes de perder el acceso: no existe un alta anónima de emergencia. Si todavía no hay cuenta o respaldo válido, debe intervenir el responsable de SQL/Windows; no se inventan usuarios ni se omiten las verificaciones.

## Recuperación

Al abrir el inicio de sesión, el sistema verifica si se necesita la contingencia. Sólo en ese caso redirige automáticamente a la pantalla de recuperación para ingresar las credenciales de emergencia. El login no muestra un botón ni un enlace de recuperación del WebMaster. Sólo se admite el acceso cuando hay inconsistencias de integridad, no queda un WebMaster confiable, SQL no está disponible o hay mantenimiento pendiente. El acceso directo por URL o POST se revalida antes de ejecutar eventos y vuelve al login cuando la contingencia no corresponde. La cuenta no se almacena en SQL ni obtiene el rol WebMaster normal.

Seleccionar un punto XML y confirmar. Se perderán los cambios posteriores al punto elegido. El sistema reconstruye una base temporal con las migraciones del proyecto, importa todos los datos relacionados y valida las claves foráneas, DVH/DVV, referencias por campo y la existencia de un WebMaster activo y confiable. Exige la misma cuenta de contingencia, el mismo nombre de base de datos y migraciones compatibles (la versión vigente o la inmediatamente anterior a la ampliación de integridad). El servidor de origen se conserva como dato informativo y puede diferir del servidor de destino. Sólo después de validar reemplaza la base de destino, restaura el histórico de bitácora y audita la recuperación.

La reconstrucción usa un `.bak` transitorio generado desde los datos XML validados para aplicar el reemplazo con SQL Server. No depende de que exista previamente un backup binario ni de que las tablas o procedimientos originales sigan disponibles. Se eliminan la base temporal y el archivo transitorio al terminar.

Se invalidan todas las sesiones. Una recuperación exitosa termina el mantenimiento y exige un nuevo inicio de sesión del WebMaster original. Si falla después de iniciar el reemplazo, queda en mantenimiento; la pantalla de emergencia sigue accesible para autenticar nuevamente y reintentar. La aplicación no habilita los módulos normales mientras la recuperación está pendiente.

## Protección y arquitectura

- **BE:** contratos de cuenta y respaldo de contingencia.
- **BLL:** condiciones de activación, autorización, generación, verificación y coordinación de restauración.
- **DAL:** XML externo, ACL, exportación/importación de datos y operaciones SQL; reutiliza las migraciones y `BackupDAL`.
- **SECURITY:** verificación PBKDF2-HMAC-SHA256 con sal aleatoria de 32 bytes, 210.000 iteraciones y resultado de 32 bytes, comparación constante y sesión independiente.
- **GUI:** configuración reservada al WebMaster y pantalla `RecuperacionWebMaster.aspx`; `Global.asax` bloquea GET/POST a cualquier otro módulo durante la sesión de emergencia, antes de los eventos de página.

Los archivos están en `RutaRecuperacion/webmaster`, bajo App_Data por defecto, con permisos para la identidad del proceso, SYSTEM y administradores. `cuenta.xml` es un XML portable versionado: contiene el correo, la sal y el verificador PBKDF2 codificados en Base64, la clave HMAC y el estado de intentos. No contiene la contraseña en texto plano ni está cifrado; la confidencialidad y la protección frente a sustituciones del archivo dependen de los permisos de la carpeta. La clave HMAC permanece separada de los respaldos, dentro de la cuenta; los respaldos XML se autentican con HMAC-SHA256 y los eventos de contingencia quedan fuera de SQL. Las credenciales no se registran en auditoría. Se bloquea el acceso durante 15 minutos después de tres fallos; el contador persiste fuera de la sesión. La sesión de emergencia dura como máximo 15 minutos.

Para trasladar la recuperación, conservar y copiar juntos `cuenta.xml` y los archivos `respaldo-*.xml`. Configurar `RutaRecuperacion`, la conexión a SQL Server y los permisos de la carpeta para la identidad que ejecuta el sitio en el destino. No se requiere el perfil ni la identidad del equipo original. Mantener el mismo nombre de base de datos y las migraciones compatibles; el nombre del servidor puede cambiar. El material de la cuenta permite verificar y firmar los respaldos, por lo que no debe publicarse ni entregarse junto con exportaciones destinadas a terceros. Un archivo de cuenta de formato anterior debe convertirse antes del traslado; el sistema rechaza formatos desconocidos sin crear otra cuenta ni reemplazar sus claves.

SQL Server necesita los mismos permisos de backup/restore y creación de bases temporales que la recuperación existente, más acceso de escritura a `RutaBackups`. La identidad del sitio también debe poder leer/escribir esa carpeta y el histórico XML. Una caída del servicio SQL permite autenticar en emergencia, pero la restauración requiere que el servicio vuelva a estar disponible.

Los respaldos son puntos explícitos: no se sobrescribe automáticamente un punto sano al detectar corrupción. Las contraseñas de los usuarios normales conservan el mecanismo ya existente del proyecto.

## Permisos de la carpeta de backups

El archivo `.bak` lo escribe el servicio de SQL Server, no el usuario WebMaster ni el navegador. Un error SQL 3201 con error del sistema operativo 5 (acceso denegado) requiere dar acceso a la cuenta del servicio sobre `RutaBackups`. Los permisos SQL de backup/restore no sustituyen los permisos de Windows.

En este equipo, con la instancia local predeterminada, ejecutar desde la raíz del proyecto:

```powershell
.\Database\ConfigurarPermisosBackups.ps1
```

El script obtiene la cuenta real del servicio `MSSQLSERVER`, resuelve `RutaBackups` desde `Web.config` y agrega permiso **Modificar** heredable únicamente a esa carpeta. Conserva los demás permisos. Se puede repetir después de copiar el proyecto o recrear la carpeta; los permisos de Windows no se versionan en Git. La cuenta que ejecuta el script debe poder modificar la ACL de la carpeta; si Windows lo exige, usar PowerShell como administrador.

Para una instancia nombrada, indicar el servicio correspondiente, por ejemplo `-ServicioSql 'MSSQL$SQLEXPRESS'`. Si se aloja en IIS y la identidad del sitio todavía no tiene acceso, agregar `-IdentidadSitio 'IIS AppPool\VitaPet'`, reemplazando el nombre por el pool real. En IIS Express se utiliza la cuenta de Windows que ejecuta el sitio. Este procedimiento supone que el sitio y SQL Server están en el mismo equipo; con SQL remoto se necesita una ruta compartida accesible desde ambos servidores y permisos de recurso compartido y NTFS.

Validación realizada: creación de un backup `COPY_ONLY` con `CHECKSUM` en la carpeta configurada, `RESTORE VERIFYONLY`, restauración en una base temporal, `DBCC CHECKDB` y lectura del archivo desde Windows. Los artefactos de prueba se eliminaron al finalizar.
