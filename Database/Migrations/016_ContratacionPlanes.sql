-- No agrega tablas duplicadas: Plan, Cobertura, Cliente, Mascota y Suscripcion
-- ya existen. NULL representa el precio por convenio del plan corporativo.
ALTER TABLE dbo.[Plan] ALTER COLUMN precio DECIMAL(10,2) NULL;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_AsegurarCatalogo
    @nombre VARCHAR(80), @descripcion VARCHAR(255), @precio DECIMAL(10,2) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion.', 1;
    DECLARE @id INT=(SELECT TOP(1) idPlan FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE nombre=@nombre ORDER BY idPlan);
    DECLARE @nuevo BIT=0;
    IF @id IS NULL
    BEGIN
        INSERT dbo.[Plan](nombre,descripcion,precio,estado,dvh) VALUES(@nombre,@descripcion,@precio,1,'');
        SET @id=SCOPE_IDENTITY(); SET @nuevo=1;
    END;
    SELECT @id id, @nuevo nuevo;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_AsegurarCatalogo
    @idPlan INT, @prestacion VARCHAR(100), @detalle VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion.', 1;
    DECLARE @id INT=(SELECT TOP(1) idCobertura FROM dbo.Cobertura WITH(UPDLOCK,HOLDLOCK) WHERE idPlan=@idPlan AND prestacion=@prestacion ORDER BY idCobertura);
    DECLARE @nuevo BIT=0;
    IF @id IS NULL
    BEGIN
        INSERT dbo.Cobertura(idPlan,prestacion,detalle,estado,dvh) VALUES(@idPlan,@prestacion,@detalle,1,'');
        SET @id=SCOPE_IDENTITY(); SET @nuevo=1;
    END;
    SELECT @id id, @nuevo nuevo;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_ListarContratables
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE estado=1 AND precio>0 AND nombre IN ('Plan Esencial','Plan Preventivo','Plan Integral')
    ORDER BY precio,idPlan;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_ObtenerContratable @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE idPlan=@idPlan AND estado=1 AND precio>0
      AND nombre IN ('Plan Esencial','Plan Preventivo','Plan Integral');
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Cliente_CotizarPlan @idUsuario INT, @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idCliente,p.idPlan,p.nombre,p.descripcion,p.precio,p.dvh,
        (SELECT COUNT(*) FROM dbo.Mascota m WHERE m.idCliente=c.idCliente AND m.estado=1) cantidadMascotas
    FROM dbo.Cliente c INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
    CROSS JOIN dbo.[Plan] p
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0
      AND p.idPlan=@idPlan AND p.estado=1 AND p.precio>0
      AND p.nombre IN ('Plan Esencial','Plan Preventivo','Plan Integral');
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Suscripcion_PrepararContratacion
    @idUsuario INT, @idPlan INT, @versionPlan VARCHAR(64), @cantidadEsperada INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c WITH(UPDLOCK,HOLDLOCK)
        INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
        WHERE c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    DECLARE @version VARCHAR(64)=(SELECT dvh FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK)
        WHERE idPlan=@idPlan AND estado=1 AND precio>0 AND nombre IN ('Plan Esencial','Plan Preventivo','Plan Integral'));
    IF @version IS NULL THROW 50050, 'Plan no disponible.', 1;
    DECLARE @cantidad INT=(SELECT COUNT(*) FROM dbo.Mascota WITH(UPDLOCK,HOLDLOCK) WHERE idCliente=@cliente AND estado=1);
    IF @cantidad=0 THROW 50052, 'No existen mascotas activas.', 1;
    IF @cantidad<>@cantidadEsperada OR @version<>@versionPlan
        THROW 50053, 'Los datos de la contratacion cambiaron.', 1;
    IF EXISTS(SELECT 1 FROM dbo.Suscripcion WITH(UPDLOCK,HOLDLOCK) WHERE idCliente=@cliente AND estado='Activa' AND fechaVencimiento>=CONVERT(DATE,GETDATE()))
        THROW 50005, 'El cliente ya posee un plan activo.', 1;
    DECLARE @pendiente INT,@planPendiente INT;
    SELECT TOP(1) @pendiente=idSuscripcion,@planPendiente=idPlan FROM dbo.Suscripcion WITH(UPDLOCK,HOLDLOCK)
        WHERE idCliente=@cliente AND estado='Pendiente' ORDER BY idSuscripcion;
    IF @pendiente IS NOT NULL
    BEGIN
        IF @planPendiente<>@idPlan THROW 50005, 'Existe otra contratacion pendiente.', 1;
        SELECT @pendiente id,CONVERT(BIT,0) nuevo; RETURN;
    END;
    DECLARE @fecha DATE=CONVERT(DATE,GETDATE());
    INSERT dbo.Suscripcion(idCliente,idPlan,fechaAlta,fechaVencimiento,estado,dvh)
        VALUES(@cliente,@idPlan,@fecha,DATEADD(MONTH,1,@fecha),'Pendiente','');
    SELECT CONVERT(INT,SCOPE_IDENTITY()) id,CONVERT(BIT,1) nuevo;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Suscripcion_ObtenerPendiente @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.idSuscripcion,s.idPlan FROM dbo.Suscripcion s
    INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND s.idSuscripcion=@idSuscripcion AND s.estado='Pendiente';
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_ObtenerPorPlan @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT prestacion,detalle FROM dbo.Cobertura WHERE idPlan=@idPlan AND estado=1 ORDER BY idCobertura;
END;
