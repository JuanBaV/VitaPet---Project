# Facturación de VitaPet Rework

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
