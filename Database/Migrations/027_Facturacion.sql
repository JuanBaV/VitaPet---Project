-- Conserva Pago y sus verificadores. El detalle nuevo pertenece al cobro;
-- la emisión de Factura se confirma en una transacción posterior.
CREATE TABLE dbo.DetallePago
(
    idPago INT NOT NULL PRIMARY KEY REFERENCES dbo.Pago(idPago),
    precioMascota DECIMAL(10,2) NOT NULL,
    precioMascotaAdicional DECIMAL(10,2) NOT NULL,
    cantidadMascotas INT NOT NULL CHECK(cantidadMascotas>0),
    titular NVARCHAR(100) NULL,
    dvh VARCHAR(64) NOT NULL
);
CREATE TABLE dbo.Factura
(
    idFactura INT IDENTITY NOT NULL PRIMARY KEY,
    numeroFactura VARCHAR(15) NOT NULL UNIQUE,
    idPago INT NOT NULL UNIQUE REFERENCES dbo.Pago(idPago),
    idCliente INT NOT NULL REFERENCES dbo.Cliente(idCliente),
    idSuscripcion INT NOT NULL REFERENCES dbo.Suscripcion(idSuscripcion),
    idPlan INT NOT NULL REFERENCES dbo.[Plan](idPlan),
    clienteNombre NVARCHAR(121) NOT NULL,clienteCorreo VARCHAR(100) NOT NULL,
    planNombre VARCHAR(80) NOT NULL,concepto NVARCHAR(255) NOT NULL,
    periodoDesde DATE NOT NULL,periodoHasta DATE NOT NULL,
    fechaEmision DATETIME2 NOT NULL,
    precioMascota DECIMAL(10,2) NULL,precioMascotaAdicional DECIMAL(10,2) NULL,
    cantidadMascotas INT NULL,titular NVARCHAR(100) NULL,
    importe DECIMAL(10,2) NOT NULL CHECK(importe>0),
    moneda CHAR(3) NOT NULL CHECK(moneda='USD'),medioPago VARCHAR(40) NOT NULL,
    estado VARCHAR(30) NOT NULL CHECK(estado='Emitida'),dvh VARCHAR(64) NOT NULL
);
INSERT dbo.DVV(nombreTabla,valorDVV) VALUES
('DetallePago','e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'),
('Factura','e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855');
GO
CREATE OR ALTER PROCEDURE dbo.sp_Pago_ConsultarPendiente @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @cliente INT,@plan INT;
    SELECT @cliente=c.idCliente,@plan=s.idPlan FROM dbo.Suscripcion s WITH(UPDLOCK,HOLDLOCK)
    INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente
    WHERE s.idSuscripcion=@idSuscripcion AND c.idUsuario=@idUsuario AND c.estado=1 AND s.estado='Pendiente';
    IF @cliente IS NULL OR EXISTS(SELECT 1 FROM dbo.Pago WITH(UPDLOCK,HOLDLOCK) WHERE idSuscripcion=@idSuscripcion AND estado='Aprobado') THROW 50054,'La suscripcion no admite un nuevo pago.',1;
    DECLARE @precio DECIMAL(10,2),@nombre VARCHAR(80);
    SELECT @precio=precio,@nombre=nombre FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK)
    WHERE idPlan=@plan AND estado=1 AND precio>0 AND nombre IN ('Plan Esencial','Plan Preventivo','Plan Integral');
    IF @precio IS NULL THROW 50050,'Plan no disponible.',1;
    DECLARE @cantidad INT=(SELECT COUNT(*) FROM dbo.Mascota WITH(UPDLOCK,HOLDLOCK) WHERE idCliente=@cliente AND estado=1);
    IF @cantidad=0 THROW 50052,'No existen mascotas activas.',1;
    DECLARE @importe DECIMAL(38,2)=CONVERT(DECIMAL(20,2),@precio)+CONVERT(DECIMAL(20,2),ROUND(@precio*0.8,2))*(@cantidad-1);
    IF @importe<=0 OR @importe>99999999.99 THROW 50025,'Importe invalido.',1;
    SELECT @idSuscripcion idSuscripcion,@plan idPlan,@nombre nombre,@precio precio,@cantidad cantidadMascotas,CONVERT(DECIMAL(10,2),@importe) importe;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Pago_Registrar
    @idUsuario INT,@idSuscripcion INT,@importe DECIMAL(10,2),@medioPago VARCHAR(40),@titular NVARCHAR(100)=NULL
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50057,'Se requiere una transaccion de integridad.',1;
    DECLARE @resumen TABLE(idSuscripcion INT,idPlan INT,nombre VARCHAR(80),precio DECIMAL(10,2),cantidadMascotas INT,importe DECIMAL(10,2));
    INSERT @resumen EXEC dbo.sp_Pago_ConsultarPendiente @idUsuario,@idSuscripcion;
    IF @importe<>(SELECT importe FROM @resumen) OR @medioPago NOT IN ('Tarjeta de credito','Tarjeta de debito') THROW 50025,'Datos invalidos.',1;
    INSERT dbo.Pago(idSuscripcion,importe,medioPago,fechaPago,estado,dvh) VALUES(@idSuscripcion,@importe,@medioPago,CONVERT(DATE,GETDATE()),'Aprobado','');
    DECLARE @pago INT=SCOPE_IDENTITY();
    INSERT dbo.DetallePago(idPago,precioMascota,precioMascotaAdicional,cantidadMascotas,titular,dvh)
    SELECT @pago,precio,ROUND(precio*0.8,2),cantidadMascotas,@titular,'' FROM @resumen;
    SELECT @pago;
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
    IF EXISTS(SELECT 1 FROM dbo.Factura WITH(UPDLOCK,HOLDLOCK) WHERE idPago=@pago)
    BEGIN SELECT idFactura id,CONVERT(BIT,0) nuevo FROM dbo.Factura WHERE idPago=@pago;RETURN;END;
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
CREATE OR ALTER PROCEDURE dbo.sp_Factura_Obtener @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.* FROM dbo.Factura f INNER JOIN dbo.Cliente c ON c.idCliente=f.idCliente
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND f.idSuscripcion=@idSuscripcion;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Factura_ListarPropias @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.idSuscripcion,pl.nombre planNombre,p.importe,p.fechaPago,f.numeroFactura
    FROM dbo.Pago p INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion
    INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente INNER JOIN dbo.[Plan] pl ON pl.idPlan=s.idPlan LEFT JOIN dbo.Factura f ON f.idPago=p.idPago
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND p.estado='Aprobado' ORDER BY p.fechaPago DESC,p.idPago DESC;
END;
