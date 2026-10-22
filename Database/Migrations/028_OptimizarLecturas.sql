-- Conserva contratos, validaciones y transacciones; evita releer la misma fila.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Mascota_Actualizar
 @idUsuario INT,@idMascota INT,@dvhOriginal VARCHAR(64),
 @nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50062, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @dvhActual VARCHAR(64);
    SELECT @dvhActual=m.dvh
    FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0;
    IF @@ROWCOUNT=0 THROW 50011, 'No existen datos asociados.', 1;
    SELECT @nombre=LTRIM(RTRIM(@nombre)),@especie=LTRIM(RTRIM(@especie)),@raza=LTRIM(RTRIM(@raza));
    IF NULLIF(@nombre,'') IS NULL OR NULLIF(@especie,'') IS NULL
      OR NULLIF(@raza,'') IS NULL OR @fechaNacimiento IS NULL
      THROW 50002, 'Datos incompletos.', 1;
    IF @fechaNacimiento>CONVERT(DATE,GETDATE()) THROW 50025, 'Datos invalidos.', 1;
    IF @dvhOriginal IS NULL OR @dvhActual IS NULL OR @dvhActual<>@dvhOriginal
      THROW 50064, 'Los datos de la mascota cambiaron desde su consulta.', 1;
    UPDATE dbo.Mascota SET nombre=@nombre,especie=@especie,
      raza=@raza,fechaNacimiento=@fechaNacimiento WHERE idMascota=@idMascota;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Factura_Emitir @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50080,'Se requiere una transaccion de integridad.',1;
    DECLARE @pago INT=(SELECT TOP(1) p.idPago FROM dbo.Pago p WITH(UPDLOCK,HOLDLOCK)
    INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND s.idSuscripcion=@idSuscripcion AND p.estado='Aprobado' ORDER BY p.idPago);
    IF @pago IS NULL THROW 50054,'No existe un pago aprobado propio.',1;
    DECLARE @factura INT=(SELECT idFactura FROM dbo.Factura WITH(UPDLOCK,HOLDLOCK) WHERE idPago=@pago);
    IF @factura IS NOT NULL
    BEGIN SELECT @factura id,CONVERT(BIT,0) nuevo;RETURN;END;
    DECLARE @numero BIGINT=(SELECT ISNULL(MAX(CONVERT(BIGINT,RIGHT(numeroFactura,10))),0)+1 FROM dbo.Factura WITH(UPDLOCK,HOLDLOCK));
    IF @numero>9999999999 THROW 50080,'Numeracion agotada.',1;
    INSERT dbo.Factura(numeroFactura,idPago,idCliente,idSuscripcion,idPlan,clienteNombre,clienteCorreo,planNombre,concepto,periodoDesde,periodoHasta,fechaEmision,precioMascota,precioMascotaAdicional,cantidadMascotas,titular,importe,moneda,medioPago,estado,dvh)
    SELECT '0001-'+RIGHT('0000000000'+CONVERT(VARCHAR(10),@numero),10),p.idPago,c.idCliente,s.idSuscripcion,s.idPlan,c.nombre+' '+c.apellido,u.correoElectronico,pl.nombre,
        N'Suscripción mensual - '+pl.nombre,p.fechaPago,DATEADD(DAY,-1,DATEADD(MONTH,1,p.fechaPago)),SYSDATETIME(),d.precioMascota,d.precioMascotaAdicional,d.cantidadMascotas,d.titular,p.importe,'USD',p.medioPago,'Emitida',''
    FROM dbo.Pago p INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente
    INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario INNER JOIN dbo.[Plan] pl ON pl.idPlan=s.idPlan LEFT JOIN dbo.DetallePago d ON d.idPago=p.idPago WHERE p.idPago=@pago;
    IF @@ROWCOUNT<>1 THROW 50079,'No se pudo preparar la factura.',1;
    SELECT CONVERT(INT,SCOPE_IDENTITY()) id,CONVERT(BIT,1) nuevo;
END;
GO
