-- CU-005-005: valor cero conserva los verificadores y referencias anteriores.
IF COL_LENGTH('dbo.Usuario','requiereRestablecerContrasena') IS NULL
    ALTER TABLE dbo.Usuario ADD requiereRestablecerContrasena BIT NOT NULL
        CONSTRAINT DF_Usuario_Restablecer DEFAULT(0) WITH VALUES;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerPorCorreo @correoElectronico VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Usuario WHERE correoElectronico=@correoElectronico;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerParaIntegridad
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Usuario ORDER BY idUsuario;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerPorId @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT * FROM dbo.Usuario WHERE idUsuario=@idUsuario;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ActualizarContrasena @idUsuario INT,@contrasena VARCHAR(255),@dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    UPDATE dbo.Usuario SET contrasena=@contrasena,requiereRestablecerContrasena=0,dvh=@dvh WHERE idUsuario=@idUsuario;
    IF @@ROWCOUNT<>1 THROW 50002,'No existe el usuario indicado.',1;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ClienteCorporativo_ConsultarPlan @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50074,'Se requiere una transaccion de integridad.',1;
    SELECT TOP(1) idPlan,nombre FROM dbo.[Plan] WHERE nombre='Plan Corporativo' AND estado=1 ORDER BY idPlan;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ClienteCorporativo_CorreoRegistrado @idUsuario INT,@correo VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50074,'Se requiere una transaccion de integridad.',1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario WITH(UPDLOCK,HOLDLOCK) WHERE correoElectronico=@correo) THEN 1 ELSE 0 END);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ClienteCorporativo_RegistrarUsuario
    @idAdministrador INT,@nombre NVARCHAR(MAX),@correo NVARCHAR(MAX),@hash VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idAdministrador AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@correo)),N'') IS NULL THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>100 OR LEN(@correo)>100 OR LEN(@hash)<>64 OR @hash IS NULL OR @hash COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%'
        THROW 50025,'Datos invalidos.',1;
    IF @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
       OR @correo COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@correo)) COLLATE Latin1_General_100_BIN2
        THROW 50025,'Datos invalidos.',1;
    IF EXISTS(SELECT 1 FROM dbo.Usuario WITH(UPDLOCK,HOLDLOCK) WHERE correoElectronico=@correo) THROW 50001,'Correo ya registrado.',1;
    BEGIN TRY
        INSERT dbo.Usuario(nombre,correoElectronico,contrasena,rol,estado,intentosFallidos,bloqueado,idioma,requiereRestablecerContrasena,dvh)
        OUTPUT inserted.idUsuario VALUES(@nombre,@correo,@hash,'Cliente',1,0,0,'es',1,'');
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50001,'Correo ya registrado.',1;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ClienteCorporativo_RegistrarCliente
    @idAdministrador INT,@idUsuario INT,@idEmpresa INT,@nombre NVARCHAR(MAX),@apellido NVARCHAR(MAX),@telefono NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idAdministrador AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empresa WHERE idEmpresa=@idEmpresa) THROW 50011,'No existen datos asociados.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empresa WHERE idEmpresa=@idEmpresa AND estado=1) THROW 50072,'La empresa seleccionada se encuentra inactiva.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Cliente' AND estado=1 AND requiereRestablecerContrasena=1)
       OR EXISTS(SELECT 1 FROM dbo.Cliente WHERE idUsuario=@idUsuario) THROW 50049,'Cuenta no disponible para el alta corporativa.',1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@apellido)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@telefono)),N'') IS NULL THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>60 OR LEN(@apellido)>60 OR LEN(@nombre+N' '+@apellido)>100 OR LEN(@telefono)>30 THROW 50025,'Datos invalidos.',1;
    INSERT dbo.Cliente(idUsuario,idEmpresa,nombre,apellido,telefono,tipoCliente,estado,dvh)
    OUTPUT inserted.idCliente VALUES(@idUsuario,@idEmpresa,@nombre,@apellido,@telefono,'Corporativo',1,'');
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
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan AND nombre='Plan Corporativo' AND estado=1) THROW 50073,'No se encuentra disponible el plan Corporativo.',1;
    IF EXISTS(SELECT 1 FROM dbo.Suscripcion WHERE idCliente=@idCliente) THROW 50007,'El cliente ya posee una suscripcion.',1;
    DECLARE @hoy DATE=CONVERT(DATE,GETDATE());
    INSERT dbo.Suscripcion(idCliente,idPlan,fechaAlta,fechaVencimiento,estado,dvh)
    OUTPUT inserted.idSuscripcion VALUES(@idCliente,@idPlan,@hoy,DATEADD(MONTH,1,@hoy),'Activa','');
END;
