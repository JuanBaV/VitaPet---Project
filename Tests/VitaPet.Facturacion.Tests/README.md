# Facturación de VitaPet Rework

Para el punto 18, compilar el proyecto de pruebas y ejecutar:

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --accesos
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 'Accesos-Web.cjs' '..\..\artifacts\punto18-accesos'
```

La primera prueba comprueba la matriz de permisos del catálogo para los tres
perfiles iniciales y usuarios sin sesión. Verifica el rechazo desde la BLL de
altas de usuarios y perfiles, backups, restauraciones y altas de planes sin
permiso, conserva los conteos de registros y comprueba la integridad.
El recorrido web intenta las URL prohibidas por GET, con y sin `.aspx`, y
por POST directo antes de validar el formulario. Comprueba el acceso propio,
la consulta de perfiles compartida con Webmaster, las pantallas de rechazo,
el regreso al inicio y el acceso después de cerrar sesión. La matriz debe
coincidir con las 32 pantallas del catálogo; una incorporación exige ampliar
la cobertura. Ambas pruebas crean y eliminan su propia base temporal.

Para el punto 17:

```powershell
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 'Navegacion-Web.cjs' '..\..\artifacts\punto17-navegacion'
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 'Interfaz-Web.cjs' '..\..\artifacts\punto17-interfaz'
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 'Backup-Confirmacion-Web.cjs' '..\..\artifacts\punto17-confirmacion-backup'
```

El primer recorrido verifica los regresos, el teclado, los filtros conservados
y la navegación móvil en español e inglés. El segundo comprueba los enlaces
de los menús públicos y de los tres perfiles, títulos, etiquetas, imágenes,
recursos y disposición en anchos de 360, 390, 768, 1024 y 1440 píxeles.
Incluye cancelar y confirmar el cierre de sesión con teclado. Guarda capturas
y `interfaz-resultados.json`; falla si una opción del menú queda fuera de la
pantalla, aunque el documento no muestre una barra horizontal. Ambos usan
una base temporal y conservan el recorrido de facturación existente.

El recorrido de confirmaciones comprueba que cada panel aparezca debajo de
su botón, que solo haya uno visible y que pueda cancelarse o cambiarse de
operación, en 1440, 768 y 390 píxeles. Usa metadatos sintéticos de un respaldo
solo para abrir el diálogo: no ejecuta backups ni restauraciones.

Para el punto 16, después de compilar la solución, ejecutar:

```powershell
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 'Altas-Web.cjs' '..\..\artifacts\punto16' 3
```

Comprueba los siete formularios de alta (registro público, usuario, mascota,
empresa, plan con coberturas, cliente corporativo y perfil) con JavaScript
activado y desactivado: obligatorios, formatos, longitudes y altas válidas.
Incluye inglés y móvil en las validaciones nuevas, la selección incompleta
de turnos y el recorrido de facturación existente. Las tres suscripciones
esperadas corresponden al pago y a las dos altas corporativas de la prueba.

El punto 15 se verifica con `--contrasenas`. Prueba el rechazo de claves
vacías y de 1 a 7 caracteres en registro público, alta del Webmaster,
restablecimiento y configuración de emergencia. Comprueba altas y cambios
válidos de 8 y 100 caracteres, complejidad, hash e integridad en la misma
base temporal aislada. La emergencia conserva su mínimo de 12 caracteres.

Los puntos 12–14 se verifican ejecutando el mismo programa con `--perfiles`.
Comprueba el Composite de perfiles/pantallas, herencia, ciclos, versiones,
altas del Webmaster, restricciones del Administrador y registro público con
rol Cliente fijo. El alta pública no admite un perfil enviado por el cliente.
Las pruebas crean y eliminan su propia base SQL temporal.

Compilar VitaPet.Facturacion.Tests.csproj con MSBuild de Visual Studio y ejecutar
bin/Debug/net48/VitaPet.Facturacion.Tests.exe. La suite usa y elimina su propia
base VitaPet_Verificacion_Rework_<guid>; no modifica VitaPet.

Run-Web.ps1 ejecuta otra fixture aislada y una copia del sitio fuera de OneDrive.
Comprueba el simulador dummy existente, el descuento, la emisión y la descarga,
el acceso posterior desde Planes y Mi perfil, la consulta de cobertura, móvil,
inglés y el rechazo de facturas ajenas. Detiene IIS Express y elimina su base.
Las capturas y PDF quedan en web-output.

Para comprobar los controles «← Volver» de los formularios secundarios:

```powershell
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 'Navegacion-Web.cjs' '..\..\artifacts\navegacion-web'
```

Reutiliza el recorrido de facturación y comprueba los destinos de regreso,
teclado, posición del control, móvil e inglés con los distintos roles. La
fixture web asigna `Prueba2026!` a sus usuarios de prueba. Las capturas y el
detalle de los resultados quedan en `artifacts/navegacion-web`.

La migración 027 conserva los pagos y sus verificadores. Agrega DetallePago
para los cobros nuevos y Factura, protegidos por DVH, DVV y referencias. El
pago y su detalle se confirman juntos; la emisión y su auditoría usan otra
transacción. Solo existe una factura por pago. El reenvío de un pago aprobado
recupera su resultado y no llama nuevamente al simulador.

Cada mascota adicional recibe un 20 % de descuento, con redondeo a dos
decimales por precio unitario. La primera paga el precio completo. La factura
conserva los precios, la cantidad y el titular del cobro; nunca guarda PAN,
vencimiento ni código de seguridad.

Los pagos anteriores pueden facturarse al consultar su comprobante desde
Mi perfil. Conservan su importe registrado. Como no guardaban titular ni el
detalle por mascota, el PDF identifica esa ausencia y no reconstruye un
descuento histórico con valores actuales. Es un PDF de demostración del TFI,
sin autorización fiscal.

El simulador genera un certificado X.509 autofirmado y una autorización CMS
con firma RSA y SHA-256. La clave privada permanece en la carpeta local
CertificadosPago, junto a RutaBitacoraXml. El cliente solo descarga el CER
público y el P7M firmado de su propio pago. No se instala una autoridad de
confianza en Windows ni se integra un procesador bancario.

Para ejecutar las pruebas criptográficas sin usar SQL Server:

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --certificados
```

Comprueban la firma, la alteración del importe y del contenido CMS, otro
certificado, la persistencia y la reutilización de IDs después de restaurar.

El correo se envía después de confirmar la factura. Un fallo SMTP no revierte
el pago ni la factura. Se conserva un estado local protegido por DPAPI para
no repetir automáticamente un envío aceptado o cuyo resultado sea incierto.
El comprobante permite reintentar un correo pendiente sin volver a cobrar.

`Scripts/ConfigurarCorreo.ps1` guarda la contraseña de aplicación de Gmail
cifrada para el usuario de Windows que ejecuta IIS Express. El archivo
`VitaPet Rework/Correo.local.config` está excluido de Git. Solo configura el
remitente y su credencial; no fija el destinatario de las facturas.
En otro equipo o con otra identidad de IIS se debe volver a configurar.

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --correo
```

Esta prueba usa el directorio de captura SMTP de .NET; comprueba remitente,
destinatario, adjunto PDF, repetición y reintento. No contacta Gmail ni prueba
recepción real. La aceptación de Gmail se comprueba por separado y la
recepción la confirma el usuario.

El formulario de pago reutiliza `ValidacionEmail`, exige un correo de hasta
100 caracteres y lo valida también en BLL antes de registrar el cobro.
La migración 029 conserva ese correo en DetallePago con sus verificadores.
Los detalles anteriores, con correo NULL, conservan sus dígitos originales.
El envío y sus reintentos usan siempre el correo elegido en ese pago; el
correo del perfil se mantiene como dato del cliente. Las facturas anteriores
sin destinatario elegido no generan nuevos envíos automáticos.

El punto 20 incorpora cifrado AES-256 y HMAC-SHA256 de los 14 campos personales
de Usuario, Cliente, Empresa, DetallePago y Factura. La base del sitio ya está
cifrada; la preparación de fixtures pertenece sólo a los ejecutables de pruebas.
La aplicación no convierte bases al iniciar. La clave y la configuración en otra
PC se describen en `GUIA_CIFRADO.md`.

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --cifrado
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Web.cjs cifrado-web-output 1 cifrado
```

La primera variante prueba el almacenamiento, las escrituras, el rechazo de
claves incorrectas, la autenticación del cifrado y la restauración SQL/XML.
La segunda prepara el cifrado dentro del ejecutable de pruebas antes del
recorrido en Edge. La infraestructura sólo admite bases temporales de prueba;
la herramienta de conversión fue retirada de la aplicación.
Usa una clave de prueba propia y no modifica la clave ni la base del sitio real.

El punto 21 verifica español e inglés, incluyendo páginas públicas, los tres
perfiles, validaciones, mensajes dinámicos de perfiles, navegación móvil y
persistencia del idioma al abrir una nueva sesión. Los nombres ingresados
por los usuarios conservan su contenido original. Ambas suites rechazan
las bases que no correspondan a la preparación aislada de pruebas.

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --idioma
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Idioma-Web.cjs idioma-web-output 1 cifrado
```

El recorrido web comprueba también el cambio de idioma después de un error
de validación y la integridad de la base cifrada al finalizar. Requiere la
misma herramienta de cifrado compilada que el recorrido del punto 20.
La preparación web captura los correos en una carpeta local de la base
temporal y excluye la configuración privada de correo del sitio real.

La suite de controles del punto 22 comprueba cinco de los ocho .ascx de Controls:
ValidacionEmail, ValidacionNumerica, SelectorMascota, FiltroZona y
ListadoPlanes. El validador numérico se reutiliza en el alta y la
modificación de planes; acepta coma o punto decimal y conserva la
validación del precio en PlanBLL.

```powershell
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Controles-Web.cjs controles-web-output 1 cifrado
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Altas-Web.cjs altas-web-output 3 cifrado
```

La primera prueba verifica los validadores con y sin JavaScript, su uso
en formularios reales, el cambio de zona y mascota, la selección de
planes y la integridad final de la base temporal cifrada. La segunda
verifica los siete formularios de alta tras integrar el nuevo control.
Ese recorrido deja tres suscripciones activas: la contratación particular
inicial y las dos altas corporativas de prueba, con y sin JavaScript.

El punto 23 prueba los tres endpoints ASMX con solicitudes SOAP 1.1 y los
formularios que los consumen. Comprueba sus ocho operaciones publicadas en
WSDL, sesiones, permisos, aislamiento entre clientes, cobertura contratada,
datos clínicos y registro y consulta de turnos. Repetir la reserva debe
devolver ER013 y conservar un solo turno. Al finalizar se verifica la integridad.
El procedimiento manual y las operaciones están en [Punto-23.md](Punto-23.md).

```powershell
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Servicios-Web.cjs '..\..\artifacts\punto23-servicios' 1 cifrado --clinica
```

Compilar primero la solución y el proyecto de pruebas.
El argumento opcional `--clinica` agrega registros clínicos sintéticos de
dos clientes a la base temporal antes de cifrarla. Los recorridos anteriores
conservan su preparación habitual. No se modifican la base ni la clave del
sitio real, y los correos de la compra de preparación se capturan localmente.

El punto 24 valida los cinco patrones documentados: Singleton, Facade, State,
Template Method y Composite. El usuario aceptó revisar esos cinco frente a los
seis de la consigna original. El detalle de clases y la prueba manual están en
[Punto-24.md](Punto-24.md).

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --patrones
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Accesos-Web.cjs '..\..\artifacts\punto24-accesos' 1 cifrado
```

La primera suite agrega comprobaciones de aislamiento de sesiones, concurrencia,
transiciones de estado, persistencia y fachada de seguridad; reutiliza las pruebas
de perfiles para el Composite. La segunda repite los accesos web protegidos,
incluyendo páginas que heredan de PaginaBase. Se revisa además el método OnLoad
sellado y sus pasos de extensión en el código de la clase base.

El punto 25 verifica tres XML de negocio: Reintegros.xml, Turnos.xml y
Cobertura.xml. La Bitácora se conserva como auditoría y no se cuenta en este punto.
Se agregó la exportación del plan contratado en ConsultaCobertura, con permisos
del cliente y una nueva lectura autorizada al descargar. El detalle y la prueba
manual están en [Punto-25.md](Punto-25.md).

```powershell
& Tests/VitaPet.Facturacion.Tests/bin/Debug/net48/VitaPet.Facturacion.Tests.exe --xml
& Tests/VitaPet.Facturacion.Tests/Run-Web.ps1 Xml-Web.cjs '..\..\artifacts\punto25-xml' 1 cifrado --xml
```

La preparación XML agrega reintegros y turnos sintéticos de dos clientes y
caracteres especiales en una prestación del catálogo. La variante BLL prepara
además una contratación por el flujo existente. Se comprueban UTF-8, escape,
formato de fechas e importes, selección, contenido completo, idiomas, cabeceras
de descarga y aislamiento entre clientes. La variante web cifra la base temporal
y conserva las verificaciones de facturación del recorrido anterior.

La aplicación sin migraciones se comprueba con `--esquema-actual`: valida el
esquema en modo de solo lectura, excluye y rechaza backups anteriores, restaura
SQL/XML cifrados y reconstruye una base principal inexistente. La preparación
se describe en [SIN_MIGRACIONES.md](../../SIN_MIGRACIONES.md).