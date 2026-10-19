-- CU-006-001 a CU-006-004. Reutiliza Plan y Cobertura.
-- Falso es el valor canónico implícito: preserva los DVH y referencias anteriores.
IF COL_LENGTH('dbo.Plan','esCorporativo') IS NULL
    ALTER TABLE dbo.[Plan] ADD esCorporativo BIT NOT NULL CONSTRAINT DF_Plan_Corporativo DEFAULT(0) WITH VALUES;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_NecesitaCatalogo
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK)) THEN 0 ELSE 1 END);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_InicializarCorporativo
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE esCorporativo=1)
        UPDATE dbo.[Plan] SET esCorporativo=1,precio=COALESCE(precio,16)
        OUTPUT inserted.idPlan WHERE nombre='Plan Corporativo';
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_ListarAdministracion @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    SELECT idPlan,nombre,descripcion,precio,estado,esCorporativo FROM dbo.[Plan] ORDER BY nombre,idPlan;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_ConsultarAdministracion @idUsuario INT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan) THROW 50011,'No existen datos asociados.',1;
    SELECT idPlan,nombre,descripcion,precio,estado,esCorporativo FROM dbo.[Plan] WHERE idPlan=@idPlan;
    SELECT idCobertura,prestacion,detalle,estado FROM dbo.Cobertura WHERE idPlan=@idPlan ORDER BY prestacion,idCobertura;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_Registrar @idUsuario INT,@nombre NVARCHAR(MAX),@descripcion NVARCHAR(MAX),@precio DECIMAL(28,10),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    DECLARE @idPlan INT=0;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@descripcion)),N'') IS NULL OR @precio IS NULL OR @estado IS NULL
        THROW 50002,'Datos incompletos.',1;
    SELECT @nombre=LTRIM(RTRIM(@nombre)),@descripcion=LTRIM(RTRIM(@descripcion));
    IF LEN(@nombre)>80 OR LEN(@descripcion)>255 OR @precio<=0 OR @precio>99999999.99 OR @precio<>ROUND(@precio,2)
        THROW 50025,'Datos invalidos.',1;
    IF @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
       OR @descripcion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@descripcion)) COLLATE Latin1_General_100_BIN2
        THROW 50025,'Datos invalidos.',1;
    IF EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE nombre=@nombre AND idPlan<>@idPlan)
        THROW 50078,'El nombre del plan ya se encuentra registrado.',1;
    INSERT dbo.[Plan](nombre,descripcion,precio,estado,esCorporativo,dvh)
    OUTPUT inserted.idPlan VALUES(@nombre,@descripcion,@precio,@estado,0,'');
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_Actualizar @idUsuario INT,@nombre NVARCHAR(MAX),@descripcion NVARCHAR(MAX),@precio DECIMAL(28,10),@estado BIT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE idPlan=@idPlan) THROW 50011,'No existen datos asociados.',1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@descripcion)),N'') IS NULL OR @precio IS NULL OR @estado IS NULL
        THROW 50002,'Datos incompletos.',1;
    SELECT @nombre=LTRIM(RTRIM(@nombre)),@descripcion=LTRIM(RTRIM(@descripcion));
    IF LEN(@nombre)>80 OR LEN(@descripcion)>255 OR @precio<=0 OR @precio>99999999.99 OR @precio<>ROUND(@precio,2)
        THROW 50025,'Datos invalidos.',1;
    IF @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
       OR @descripcion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@descripcion)) COLLATE Latin1_General_100_BIN2
        THROW 50025,'Datos invalidos.',1;
    IF EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE nombre=@nombre AND idPlan<>@idPlan)
        THROW 50078,'El nombre del plan ya se encuentra registrado.',1;
    UPDATE dbo.[Plan] SET nombre=@nombre,descripcion=@descripcion,precio=@precio,estado=@estado WHERE idPlan=@idPlan;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_GuardarAdministracion @idUsuario INT,@idPlan INT,@idCobertura INT,@prestacion NVARCHAR(MAX),@detalle NVARCHAR(MAX),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan) THROW 50011,'No existen datos asociados.',1;
    IF NULLIF(LTRIM(RTRIM(@prestacion)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@detalle)),N'') IS NULL OR @estado IS NULL THROW 50002,'Datos incompletos.',1;
    SELECT @prestacion=LTRIM(RTRIM(@prestacion)),@detalle=LTRIM(RTRIM(@detalle));
    IF LEN(@prestacion)>100 OR LEN(@detalle)>255 OR @idCobertura<0 THROW 50025,'Datos invalidos.',1;
    IF @prestacion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@prestacion)) COLLATE Latin1_General_100_BIN2
       OR @detalle COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@detalle)) COLLATE Latin1_General_100_BIN2 THROW 50025,'Datos invalidos.',1;
    IF @idCobertura=0
    BEGIN
        INSERT dbo.Cobertura(idPlan,prestacion,detalle,estado,dvh)
        OUTPUT inserted.idCobertura VALUES(@idPlan,@prestacion,@detalle,@estado,'');
    END
    ELSE
    BEGIN
        IF NOT EXISTS(SELECT 1 FROM dbo.Cobertura WITH(UPDLOCK,HOLDLOCK) WHERE idCobertura=@idCobertura AND idPlan=@idPlan) THROW 50025,'Cobertura ajena al plan.',1;
        UPDATE dbo.Cobertura SET prestacion=@prestacion,detalle=@detalle,estado=@estado WHERE idCobertura=@idCobertura AND idPlan=@idPlan;
        SELECT @idCobertura;
    END;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_ListarContratables
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE estado=1 AND precio>0 AND esCorporativo=0
    ORDER BY precio,idPlan;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Plan_ObtenerContratable @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE idPlan=@idPlan AND estado=1 AND precio>0
      AND esCorporativo=0;
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
      AND p.esCorporativo=0;
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
        WHERE idPlan=@idPlan AND estado=1 AND precio>0 AND esCorporativo=0);
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
        AND esCorporativo=0;
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
CREATE OR ALTER PROCEDURE dbo.sp_ClienteCorporativo_ConsultarPlan @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50074,'Se requiere una transaccion de integridad.',1;
    SELECT TOP(1) idPlan,nombre,precio FROM dbo.[Plan] WHERE esCorporativo=1 AND estado=1 ORDER BY idPlan;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ClienteCorporativo_AsignarPlan @idAdministrador INT,@idCliente INT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idAdministrador AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50007,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Cliente c JOIN dbo.Empresa e ON e.idEmpresa=c.idEmpresa WHERE c.idCliente=@idCliente AND c.tipoCliente='Corporativo' AND c.estado=1 AND e.estado=1)
        THROW 50072,'Empresa o cliente no disponible.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan AND esCorporativo=1 AND estado=1) THROW 50073,'No se encuentra disponible el plan Corporativo.',1;
    IF EXISTS(SELECT 1 FROM dbo.Suscripcion WHERE idCliente=@idCliente) THROW 50007,'El cliente ya posee una suscripcion.',1;
    DECLARE @hoy DATE=CONVERT(DATE,GETDATE());
    INSERT dbo.Suscripcion(idCliente,idPlan,fechaAlta,fechaVencimiento,estado,dvh)
    OUTPUT inserted.idSuscripcion VALUES(@idCliente,@idPlan,@hoy,DATEADD(MONTH,1,@hoy),'Activa','');
END;
GO
