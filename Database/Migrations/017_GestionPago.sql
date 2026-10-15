-- CU-001-003 reutiliza Pago y Suscripcion. Los datos de tarjeta nunca se persisten.
CREATE OR ALTER PROCEDURE dbo.sp_Pago_ConsultarPendiente @idUsuario INT, @idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50057, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT, @plan INT;
    SELECT @cliente=c.idCliente,@plan=s.idPlan FROM dbo.Suscripcion s WITH(UPDLOCK,HOLDLOCK)
      INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente
      INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
      WHERE s.idSuscripcion=@idSuscripcion AND c.idUsuario=@idUsuario AND c.estado=1
        AND u.estado=1 AND u.bloqueado=0 AND u.rol='Cliente' AND s.estado='Pendiente';
    IF @cliente IS NULL OR EXISTS(SELECT 1 FROM dbo.Pago WITH(UPDLOCK,HOLDLOCK)
      WHERE idSuscripcion=@idSuscripcion AND estado='Aprobado')
        THROW 50054, 'La suscripcion no admite un nuevo pago.', 1;
    DECLARE @precio DECIMAL(10,2),@nombre VARCHAR(80);
    SELECT @precio=precio,@nombre=nombre FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK)
      WHERE idPlan=@plan AND estado=1 AND precio>0
        AND nombre IN ('Plan Esencial','Plan Preventivo','Plan Integral');
    IF @precio IS NULL THROW 50050, 'Plan no disponible.', 1;
    DECLARE @cantidad INT=(SELECT COUNT(*) FROM dbo.Mascota WITH(UPDLOCK,HOLDLOCK)
      WHERE idCliente=@cliente AND estado=1);
    IF @cantidad=0 THROW 50052, 'No existen mascotas activas.', 1;
    DECLARE @importe DECIMAL(38,2)=CONVERT(DECIMAL(20,2),@precio)*@cantidad;
    IF @importe<=0 OR @importe>99999999.99 THROW 50025, 'Importe invalido.', 1;
    SELECT @idSuscripcion idSuscripcion,@plan idPlan,@nombre nombre,@precio precio,
      @cantidad cantidadMascotas,CONVERT(DECIMAL(10,2),@importe) importe;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Pago_Registrar
    @idUsuario INT,@idSuscripcion INT,@importe DECIMAL(10,2),@medioPago VARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50057, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @resumen TABLE(idSuscripcion INT,idPlan INT,nombre VARCHAR(80),precio DECIMAL(10,2),cantidadMascotas INT,importe DECIMAL(10,2));
    INSERT @resumen EXEC dbo.sp_Pago_ConsultarPendiente @idUsuario,@idSuscripcion;
    IF @importe<>(SELECT importe FROM @resumen) OR @medioPago NOT IN ('Tarjeta de credito','Tarjeta de debito')
      THROW 50025, 'Datos invalidos.', 1;
    INSERT dbo.Pago(idSuscripcion,importe,medioPago,fechaPago,estado,dvh)
      OUTPUT INSERTED.idPago
      VALUES(@idSuscripcion,@importe,@medioPago,CONVERT(DATE,GETDATE()),'Aprobado','');
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Pago_ObtenerComprobante @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.idPago,p.idSuscripcion,s.idPlan,pl.nombre,p.importe,p.medioPago,p.fechaPago,p.estado
      FROM dbo.Pago p INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion
      INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente INNER JOIN dbo.[Plan] pl ON pl.idPlan=s.idPlan
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND p.idSuscripcion=@idSuscripcion AND p.estado='Aprobado'
      ORDER BY p.idPago;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Suscripcion_ActivarPagada @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT,@estado VARCHAR(30);
    SELECT @cliente=c.idCliente,@estado=s.estado FROM dbo.Suscripcion s WITH(UPDLOCK,HOLDLOCK)
      INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente
      WHERE s.idSuscripcion=@idSuscripcion AND c.idUsuario=@idUsuario AND c.estado=1;
    IF @cliente IS NULL OR @estado NOT IN ('Pendiente','Activa')
      OR NOT EXISTS(SELECT 1 FROM dbo.Pago WITH(UPDLOCK,HOLDLOCK) WHERE idSuscripcion=@idSuscripcion AND estado='Aprobado')
      THROW 50054, 'La suscripcion no admite la confirmacion del pago.', 1;
    IF @estado='Activa' BEGIN SELECT CONVERT(BIT,0) actualizado; RETURN; END;
    IF EXISTS(SELECT 1 FROM dbo.Suscripcion WITH(UPDLOCK,HOLDLOCK) WHERE idCliente=@cliente
      AND idSuscripcion<>@idSuscripcion AND estado='Activa' AND fechaVencimiento>=CONVERT(DATE,GETDATE()))
      THROW 50005, 'Existe otra suscripcion activa.', 1;
    DECLARE @fecha DATE=CONVERT(DATE,GETDATE());
    UPDATE dbo.Suscripcion SET estado='Activa',fechaAlta=@fecha,fechaVencimiento=DATEADD(MONTH,1,@fecha)
      WHERE idSuscripcion=@idSuscripcion;
    SELECT CONVERT(BIT,1) actualizado;
END;
