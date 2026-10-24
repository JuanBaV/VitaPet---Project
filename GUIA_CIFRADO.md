# Cifrado de datos y traslado de VitaPet a otra PC

## Qué se protege

Los siguientes 14 campos se guardan cifrados en las tablas físicas:

| Tabla | Campos |
| --- | --- |
| Usuario | correoElectronico, nombre |
| Cliente | nombre, apellido, telefono |
| Empresa | razonSocial, cuit, direccion, telefono |
| DetallePago | titular, correoFactura |
| Factura | clienteNombre, clienteCorreo, titular |

Se utiliza AES-256 de SQL Server, con aleatoriedad por cifrado, autenticador de
campo y un HMAC-SHA256 sobre el campo y el contenido cifrado. La marca `VP1:`
identifica el formato. No es un hash del dato personal: la aplicación puede
descifrarlo para mostrarlo, buscarlo o emitir una factura. Las contraseñas
conservan su hash; número de tarjeta, vencimiento y código de seguridad no se
persisten. Importes, fechas, estados, identificadores y catálogo de planes
conservan sus tipos para cálculos, relaciones y consultas.

El archivo externo contiene un secreto aleatorio de 32 bytes en Base64. Se
utiliza para derivar y abrir la clave AES de SQL Server y para derivar una clave
MAC independiente. El secreto no se guarda en el código, Web.config ni Git.
Las conexiones de la aplicación abren la clave sólo en su sesión SQL; las vistas
`vw_*Descifrado` recuperan los valores originales. Una sesión SQL sin las claves
no obtiene el texto original de esas vistas. DVH, DVV y referencias se verifican
sobre los valores lógicos originales, conservando los verificadores almacenados.

Requisitos: Windows, .NET Framework 4.8, Visual Studio/MSBuild y SQL Server 2019
o posterior. El cifrado desactiva la optimización de funciones escalares en la
función que genera el IV para evitar evaluaciones duplicadas del cifrado.

## Configurar el acceso a la base actual

Esta versión utiliza una base con el esquema vigente ya aplicado. El inicio
comprueba la versión y la integridad en modo de solo lectura; no convierte una
base anterior ni aplica el cifrado a datos existentes.

Conservar el archivo de clave que ya descifra la base. Su ubicación predeterminada es:

```text
%USERPROFILE%\.vitapet\cifrado.key
```

Si hace falta importar una copia existente, ejecutar desde la raíz del proyecto:

```powershell
.\Scripts\ConfigurarClaveCifrado.ps1 -ImportarDesde 'E:\TrasladoProtegido\cifrado.key'
```

El script copia la misma clave, limita su acceso al usuario de Windows y rechaza
sobrescribir un archivo existente. No generar una clave diferente para una base
ya cifrada. Visual Studio e IIS Express deben ejecutarse con acceso al archivo.

Para una ubicación personalizada, configurar la ruta, sin incluir el secreto:

```powershell
[Environment]::SetEnvironmentVariable('VITAPET_CLAVE_ARCHIVO', 'D:\ClavesPrivadas\VitaPet\cifrado.key', 'User')
$env:VITAPET_CLAVE_ARCHIVO = 'D:\ClavesPrivadas\VitaPet\cifrado.key'
```

Reiniciar Visual Studio/IIS Express después de cambiar esa variable. Se conserva
la ubicación fuera de AppData para evitar diferencias por virtualización MSIX.
La herramienta que aplicaba el cifrado y su script de ejecución fueron retirados.

## Trasladar el proyecto a otra PC

1. En la PC de origen, crear un backup actualizado después del cifrado.
2. Copiar el proyecto y ese backup a la PC de destino.
3. Trasladar **el mismo archivo `cifrado.key`** por un medio protegido y separado.
4. En la PC de destino, abrir PowerShell en la raíz del proyecto e importar la clave:

   ```powershell
   .\Scripts\ConfigurarClaveCifrado.ps1 -ImportarDesde 'E:\TrasladoProtegido\cifrado.key'
   ```

   Se copia al `%USERPROFILE%\.vitapet\cifrado.key` del nuevo usuario de Windows,
   con permisos adecuados para ese usuario. No se genera una clave diferente.
   Si el archivo de destino ya existe, el script se detiene: conservarlo y
   comprobar cuál corresponde a la base antes de reemplazarlo manualmente.

5. Restaurar la base en SQL Server y ajustar únicamente la conexión en Web.config
   para la instancia y el nombre de base de destino.
6. Compilar el proyecto y ejecutarlo con el usuario de Windows que importó la clave.
   Si se eligió una ruta personalizada, configurar también `VITAPET_CLAVE_ARCHIVO`
   en esa PC y reiniciar Visual Studio/IIS Express.
7. Ingresar y comprobar **Webmaster → Base de datos → Verificar integridad**.
   Consultar usuarios y facturas para confirmar la recuperación de los datos.

En IIS completo, configurar la ruta para la identidad que ejecuta el sitio y
otorgar a esa identidad lectura sobre el archivo. El archivo del perfil personal
del desarrollador no se vuelve accesible automáticamente para otro usuario o
para una cuenta de servicio. SQL Server no necesita acceso al archivo: lo lee
la aplicación. Una clave faltante, malformada o incorrecta bloquea el descifrado;
no hay una copia en texto plano de respaldo dentro de las tablas.

**Perder la clave impide recuperar los datos cifrados, aunque se conserve el
backup. Crear otra clave no recupera la anterior.** Esta versión no incorpora
rotación de claves: no reemplazar el archivo para intentar cambiarla.

## Verificación manual del punto 20

1. Ejecutar `Database\VerificarCifradoDatos.sql` en SSMS sobre VitaPet. Debe informar
   AES de 256 bits y cero valores sin la marca de cifrado en los 14 campos.
2. Consultar directamente las columnas de las tablas físicas: muestran `VP1:`
   seguido de contenido cifrado. Evitar copiar valores personales a capturas.
3. Desde el sitio, comprobar login, filtro por nombre, consulta de empresas y
   consulta/descarga PDF de una factura. Los valores deben seguir siendo legibles
   para el usuario autorizado.
4. Ejecutar **Verificar integridad**: también comprueba el MAC y el descifrado de
   cada dato almacenado. La consulta de SSMS sólo comprueba el formato externo.
5. Crear un backup nuevo y conservar el archivo de clave por separado.

Los backups SQL y XML del esquema actual conservan los campos cifrados. La
recuperación XML crea una base temporal vacía con EsquemaActual.sql y reconstruye
la clave SQL usando el mismo archivo externo. También funciona si falta la base
principal. Las versiones anteriores se rechazan antes de modificar el destino;
ya no se convierten ni se vuelven a cifrar durante la restauración.

El cifrado de columnas no convierte archivos PDF, correos ni backups históricos
en archivos cifrados, ni protege frente a quien obtenga conjuntamente la clave y
acceso a la base. Las funciones de negocio siguen mostrando los datos a usuarios
autorizados. Al trasladar la PC, la configuración de Gmail se realiza por separado
con `Scripts\ConfigurarCorreo.ps1`; esta clave sólo corresponde a los datos de SQL.

## Pruebas automatizadas

```powershell
& '.\artifacts\sin-migraciones-build\VitaPet.Facturacion.Tests.exe' --cifrado
```

La suite usa bases temporales, crea una clave de prueba y las elimina al terminar.
La preparación de los datos cifrados ocurre sólo en el ejecutable de pruebas.
Comprueba rollback del fixture, un vector HMAC-SHA256 de RFC 4231, conservación de
Unicode, altas, modificaciones, duplicados, pago, PDF, autenticación del cifrado,
clave importada/incorrecta/ausente y restauración SQL y XML. No envía correos
reales ni utiliza tarjetas reales.

La compilación aislada y las pruebas de la aplicación sin migraciones se describen
en [SIN_MIGRACIONES.md](SIN_MIGRACIONES.md).

Referencias: [ENCRYPTBYKEY de Microsoft](https://learn.microsoft.com/en-us/sql/t-sql/functions/encryptbykey-transact-sql),
[almacenamiento criptográfico de OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html),
[RFC 4231](https://www.rfc-editor.org/rfc/rfc4231).
