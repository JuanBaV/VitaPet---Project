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
sobre los valores lógicos originales, sin recalcularlos durante la migración.

Requisitos: Windows, .NET Framework 4.8, Visual Studio/MSBuild y SQL Server 2019
o posterior. El cifrado desactiva la optimización de funciones escalares en la
función que genera el IV para evitar evaluaciones duplicadas del cifrado.

## Primera configuración en una instalación sin cifrar

Abrir PowerShell en la raíz de `VitaPet Rework` y ejecutar:

```powershell
.\Scripts\ConfigurarClaveCifrado.ps1
```

El destino predeterminado es:

```text
%USERPROFILE%\.vitapet\cifrado.key
```

El script genera una clave criptográficamente aleatoria, restringe el acceso al
usuario de Windows que lo ejecuta y no muestra su contenido. Rechaza sobrescribir
un archivo existente. Guardar una copia protegida de este archivo, separada del
backup de la base. No compartirlo por correo ni agregarlo al repositorio.

Se usa una carpeta del perfil fuera de `AppData` para que Visual Studio y las
herramientas empaquetadas (MSIX) lean el mismo archivo. Windows puede virtualizar
`LocalAppData` y mantener dos archivos diferentes bajo una ruta aparente idéntica.
Si la base ya está cifrada con una versión anterior, importar la clave existente
que efectivamente la descifra; no generar otra. Conservar las copias anteriores
hasta comprobar login e integridad desde Visual Studio.

Si se necesita otra ubicación, especificar la ruta del archivo y configurar la
variable de entorno `VITAPET_CLAVE_ARCHIVO` con esa misma ruta:

```powershell
.\Scripts\ConfigurarClaveCifrado.ps1 -Archivo 'D:\ClavesPrivadas\VitaPet\cifrado.key'
[Environment]::SetEnvironmentVariable('VITAPET_CLAVE_ARCHIVO', 'D:\ClavesPrivadas\VitaPet\cifrado.key', 'User')
$env:VITAPET_CLAVE_ARCHIVO = 'D:\ClavesPrivadas\VitaPet\cifrado.key'
```

La variable contiene una ruta, no la clave. Reiniciar Visual Studio/IIS Express
cuando se cambie una variable de entorno: los procesos abiertos conservan el
entorno anterior. Con la ruta predeterminada no hace falta definir la variable.

## Aplicar el cifrado a una base existente

Cerrar el sitio y detener su ejecución en Visual Studio durante este paso.
Revisar que `VitaPet Rework\Web.config` apunte a la base correcta.

```powershell
.\Scripts\AplicarCifrado.ps1 -RespaldoPrevio 'C:\RutaConPermisoSQL\antes_cifrado_20261023.bak'
```

La ruta debe ser absoluta y nueva. La cuenta del servicio SQL Server necesita
permiso para escribir allí. Puede utilizarse la carpeta de backups del sitio
si ya se configuraron esos permisos. La herramienta:

1. Compila los componentes y actualiza los DLL del sitio.
2. Comprueba la integridad de la base.
3. Crea un backup previo, verifica su checksum, lo restaura en una base temporal
   y comprueba la integridad de esa restauración. Elimina la base temporal.
4. Cifra los campos y adapta los procedimientos dentro de una transacción.
5. Comprueba que los datos recuperados coincidan con sus DVH, DVV y referencias
   originales. Ante una discrepancia, revierte la migración completa.

No se ejecuta esta migración automáticamente al iniciar el sitio. Repetir la
herramienta sobre una base ya cifrada sólo verifica su integridad. No cambia la
clave ni vuelve a cifrar los campos. El backup previo conserva el formato sin
cifrar: protegerlo y conservarlo según la política de respaldos.

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

Los backups SQL y XML creados después de la migración conservan los campos
cifrados. La recuperación XML reconstruye la clave SQL en una base temporal
usando el mismo archivo externo. Los backups anteriores conservan su formato
original: si se restaura uno sin cifrar, repetir el paso explícito de aplicación
del cifrado antes de continuar utilizando esa base.

El cifrado de columnas no convierte archivos PDF, correos ni backups históricos
en archivos cifrados, ni protege frente a quien obtenga conjuntamente la clave y
acceso a la base. Las funciones de negocio siguen mostrando los datos a usuarios
autorizados. Al trasladar la PC, la configuración de Gmail se realiza por separado
con `Scripts\ConfigurarCorreo.ps1`; esta clave sólo corresponde a los datos de SQL.

## Pruebas automatizadas

```powershell
& 'Tests\VitaPet.Facturacion.Tests\bin\Debug\net48\VitaPet.Facturacion.Tests.exe' --cifrado
```

La suite usa bases temporales, crea una clave de prueba y las elimina al terminar.
Comprueba migración/rollback, un vector HMAC-SHA256 de RFC 4231, conservación de
Unicode, altas, modificaciones, duplicados, pago, PDF, autenticación del cifrado,
clave importada/incorrecta/ausente y restauración SQL y XML. No envía correos
reales ni utiliza tarjetas reales.

Referencias: [ENCRYPTBYKEY de Microsoft](https://learn.microsoft.com/en-us/sql/t-sql/functions/encryptbykey-transact-sql),
[almacenamiento criptográfico de OWASP](https://cheatsheetseries.owasp.org/cheatsheets/Cryptographic_Storage_Cheat_Sheet.html),
[RFC 4231](https://www.rfc-editor.org/rfc/rfc4231).
