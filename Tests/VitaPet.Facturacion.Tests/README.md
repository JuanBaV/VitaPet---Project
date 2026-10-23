# Facturación de VitaPet Rework

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
