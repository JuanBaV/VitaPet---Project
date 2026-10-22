-- NULL conserva los detalles históricos sin inventar un destinatario elegido en el pago.
ALTER TABLE dbo.DetallePago ADD correoFactura VARCHAR(100) NULL;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Pago_Registrar
    @idUsuario INT,@idSuscripcion INT,@importe DECIMAL(10,2),@medioPago VARCHAR(40),
    @titular NVARCHAR(100)=NULL,@correoFactura VARCHAR(100)=NULL
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50057,'Se requiere una transaccion de integridad.',1;
    IF @correoFactura IS NULL OR LEN(LTRIM(RTRIM(@correoFactura)))=0 THROW 50025,'Correo de factura requerido.',1;
    DECLARE @resumen TABLE(idSuscripcion INT,idPlan INT,nombre VARCHAR(80),precio DECIMAL(10,2),cantidadMascotas INT,importe DECIMAL(10,2));
    INSERT @resumen EXEC dbo.sp_Pago_ConsultarPendiente @idUsuario,@idSuscripcion;
    IF @importe<>(SELECT importe FROM @resumen) OR @medioPago NOT IN ('Tarjeta de credito','Tarjeta de debito') THROW 50025,'Datos invalidos.',1;
    INSERT dbo.Pago(idSuscripcion,importe,medioPago,fechaPago,estado,dvh) VALUES(@idSuscripcion,@importe,@medioPago,CONVERT(DATE,GETDATE()),'Aprobado','');
    DECLARE @pago INT=SCOPE_IDENTITY();
    INSERT dbo.DetallePago(idPago,precioMascota,precioMascotaAdicional,cantidadMascotas,titular,correoFactura,dvh)
    SELECT @pago,precio,ROUND(precio*0.8,2),cantidadMascotas,@titular,@correoFactura,'' FROM @resumen;
    SELECT @pago;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Factura_Obtener @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.*,d.correoFactura FROM dbo.Factura f INNER JOIN dbo.Cliente c ON c.idCliente=f.idCliente
    LEFT JOIN dbo.DetallePago d ON d.idPago=f.idPago
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND f.idSuscripcion=@idSuscripcion;
END;
