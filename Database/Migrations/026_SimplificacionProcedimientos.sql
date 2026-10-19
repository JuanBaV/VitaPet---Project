-- Simplificacion de procedimientos sin cambios de contrato.
-- Conserva permisos, transacciones, bloqueos, errores y controles de integridad.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
       OR @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
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

CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_GuardarAdministracion @idUsuario INT,@idPlan INT,@idCobertura INT,@prestacion NVARCHAR(MAX),@detalle NVARCHAR(MAX),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan) THROW 50011,'No existen datos asociados.',1;
    SELECT @prestacion=LTRIM(RTRIM(@prestacion)),@detalle=LTRIM(RTRIM(@detalle));
    IF NULLIF(@prestacion,N'') IS NULL OR NULLIF(@detalle,N'') IS NULL OR @estado IS NULL THROW 50002,'Datos incompletos.',1;
    IF LEN(@prestacion)>100 OR LEN(@detalle)>255 OR @idCobertura<0
       OR @prestacion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@prestacion)) COLLATE Latin1_General_100_BIN2
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

CREATE OR ALTER PROCEDURE dbo.sp_Empresa_Actualizar @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX),@idEmpresa INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50070, 'Se requiere una transaccion de integridad.', 1;
    SELECT @razonSocial=LTRIM(RTRIM(@razonSocial)),@cuit=LTRIM(RTRIM(@cuit)),@direccion=LTRIM(RTRIM(@direccion)),@telefono=LTRIM(RTRIM(@telefono));
    IF NULLIF(@razonSocial,N'') IS NULL OR NULLIF(@cuit,N'') IS NULL
       OR NULLIF(@direccion,N'') IS NULL OR NULLIF(@telefono,N'') IS NULL
        THROW 50002, 'Datos incompletos.', 1;
    IF LEN(@razonSocial)>100 OR LEN(@cuit)>20 OR LEN(@direccion)>150 OR LEN(@telefono)>30
        THROW 50025, 'Datos invalidos.', 1;
    IF NOT(LEN(@cuit)=11 OR (LEN(@cuit)=13 AND SUBSTRING(@cuit,3,1)=N'-' AND SUBSTRING(@cuit,12,1)=N'-'))
        THROW 50025, 'Datos invalidos.', 1;
    SET @cuit=REPLACE(@cuit,N'-',N'');
    IF LEN(@cuit)<>11 OR @cuit COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9 +()-]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[0-9]%'
       OR @razonSocial COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@razonSocial)) COLLATE Latin1_General_100_BIN2
       OR @direccion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@direccion)) COLLATE Latin1_General_100_BIN2
        THROW 50025, 'Datos invalidos.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empresa WITH(UPDLOCK,HOLDLOCK) WHERE idEmpresa=@idEmpresa)
        THROW 50011, 'No existen datos asociados.', 1;
    IF EXISTS(SELECT 1 FROM dbo.Empresa WITH(UPDLOCK,HOLDLOCK) WHERE REPLACE(cuit,'-','')=@cuit AND idEmpresa<>@idEmpresa)
        THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
    BEGIN TRY
        -- Conserva identificador, estado y asociaciones. Sin comparacion de versiones.
        UPDATE dbo.Empresa SET razonSocial=@razonSocial,cuit=@cuit,direccion=@direccion,telefono=@telefono WHERE idEmpresa=@idEmpresa;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Empresa_Registrar @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50069, 'Se requiere una transaccion de integridad.', 1;
    SELECT @razonSocial=LTRIM(RTRIM(@razonSocial)),@cuit=LTRIM(RTRIM(@cuit)),@direccion=LTRIM(RTRIM(@direccion)),@telefono=LTRIM(RTRIM(@telefono));
    IF NULLIF(@razonSocial,N'') IS NULL OR NULLIF(@cuit,N'') IS NULL
       OR NULLIF(@direccion,N'') IS NULL OR NULLIF(@telefono,N'') IS NULL
        THROW 50002, 'Datos incompletos.', 1;
    IF LEN(@razonSocial)>100 OR LEN(@cuit)>20 OR LEN(@direccion)>150 OR LEN(@telefono)>30
        THROW 50025, 'Datos invalidos.', 1;
    IF NOT(LEN(@cuit)=11 OR (LEN(@cuit)=13 AND SUBSTRING(@cuit,3,1)=N'-' AND SUBSTRING(@cuit,12,1)=N'-'))
        THROW 50025, 'Datos invalidos.', 1;
    SET @cuit=REPLACE(@cuit,N'-',N'');
    IF LEN(@cuit)<>11 OR @cuit COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9 +()-]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[0-9]%'
       OR @razonSocial COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@razonSocial)) COLLATE Latin1_General_100_BIN2
       OR @direccion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@direccion)) COLLATE Latin1_General_100_BIN2
        THROW 50025, 'Datos invalidos.', 1;
    IF EXISTS(SELECT 1 FROM dbo.Empresa WITH(UPDLOCK,HOLDLOCK) WHERE REPLACE(cuit,'-','')=@cuit)
        THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
    BEGIN TRY
        INSERT dbo.Empresa(razonSocial,cuit,direccion,telefono,estado,dvh)
        OUTPUT inserted.idEmpresa VALUES(@razonSocial,@cuit,@direccion,@telefono,1,'');
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Mascota_Actualizar
 @idUsuario INT,@idMascota INT,@dvhOriginal VARCHAR(64),
 @nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50062, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT @nombre=LTRIM(RTRIM(@nombre)),@especie=LTRIM(RTRIM(@especie)),@raza=LTRIM(RTRIM(@raza));
    IF NULLIF(@nombre,'') IS NULL OR NULLIF(@especie,'') IS NULL
      OR NULLIF(@raza,'') IS NULL OR @fechaNacimiento IS NULL
      THROW 50002, 'Datos incompletos.', 1;
    IF @fechaNacimiento>CONVERT(DATE,GETDATE()) THROW 50025, 'Datos invalidos.', 1;
    IF @dvhOriginal IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Mascota WHERE idMascota=@idMascota AND dvh=@dvhOriginal)
      THROW 50064, 'Los datos de la mascota cambiaron desde su consulta.', 1;
    UPDATE dbo.Mascota SET nombre=@nombre,especie=@especie,
      raza=@raza,fechaNacimiento=@fechaNacimiento WHERE idMascota=@idMascota;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Mascota_Registrar
    @idUsuario INT,@nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50060, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT @nombre=LTRIM(RTRIM(@nombre)),@especie=LTRIM(RTRIM(@especie)),@raza=LTRIM(RTRIM(@raza));
    IF NULLIF(@nombre,'') IS NULL OR NULLIF(@especie,'') IS NULL
      OR NULLIF(@raza,'') IS NULL OR @fechaNacimiento IS NULL
      THROW 50002, 'Datos incompletos.', 1;
    IF @fechaNacimiento>CONVERT(DATE,GETDATE()) THROW 50025, 'Datos invalidos.', 1;
    INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh)
      VALUES(@cliente,@nombre,@especie,@raza,@fechaNacimiento,1,'');
    SELECT CONVERT(INT,SCOPE_IDENTITY()) idMascota;
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
    SELECT @nombre=LTRIM(RTRIM(@nombre)),@descripcion=LTRIM(RTRIM(@descripcion));
    IF NULLIF(@nombre,N'') IS NULL OR NULLIF(@descripcion,N'') IS NULL OR @precio IS NULL OR @estado IS NULL
        THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>80 OR LEN(@descripcion)>255 OR @precio<=0 OR @precio>99999999.99 OR @precio<>ROUND(@precio,2)
       OR @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
       OR @descripcion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@descripcion)) COLLATE Latin1_General_100_BIN2
        THROW 50025,'Datos invalidos.',1;
    IF EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE nombre=@nombre AND idPlan<>@idPlan)
        THROW 50078,'El nombre del plan ya se encuentra registrado.',1;
    UPDATE dbo.[Plan] SET nombre=@nombre,descripcion=@descripcion,precio=@precio,estado=@estado WHERE idPlan=@idPlan;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Plan_AsegurarCatalogo
    @nombre VARCHAR(80), @descripcion VARCHAR(255), @precio DECIMAL(10,2) = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion.', 1;
    DECLARE @id INT=(SELECT idPlan FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE nombre=@nombre);
    DECLARE @nuevo BIT=0;
    IF @id IS NULL
    BEGIN
        INSERT dbo.[Plan](nombre,descripcion,precio,estado,dvh) VALUES(@nombre,@descripcion,@precio,1,'');
        SET @id=SCOPE_IDENTITY(); SET @nuevo=1;
    END;
    SELECT @id id, @nuevo nuevo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Plan_Registrar @idUsuario INT,@nombre NVARCHAR(MAX),@descripcion NVARCHAR(MAX),@precio DECIMAL(28,10),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    SELECT @nombre=LTRIM(RTRIM(@nombre)),@descripcion=LTRIM(RTRIM(@descripcion));
    IF NULLIF(@nombre,N'') IS NULL OR NULLIF(@descripcion,N'') IS NULL OR @precio IS NULL OR @estado IS NULL
        THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>80 OR LEN(@descripcion)>255 OR @precio<=0 OR @precio>99999999.99 OR @precio<>ROUND(@precio,2)
       OR @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
       OR @descripcion COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@descripcion)) COLLATE Latin1_General_100_BIN2
        THROW 50025,'Datos invalidos.',1;
    IF EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE nombre=@nombre AND idPlan<>0)
        THROW 50078,'El nombre del plan ya se encuentra registrado.',1;
    INSERT dbo.[Plan](nombre,descripcion,precio,estado,esCorporativo,dvh)
    OUTPUT inserted.idPlan VALUES(@nombre,@descripcion,@precio,@estado,0,'');
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Reintegro_ConsultarCliente @idUsuario INT,@idReintegro INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50059, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Reintegro WHERE idReintegro=@idReintegro AND idCliente=@cliente)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idReintegro,fechaSolicitud,importe,estado,detalle FROM dbo.Reintegro
      WHERE idReintegro=@idReintegro AND idCliente=@cliente;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_Listar
    @nombre NVARCHAR(100) = N'',
    @estado INT = 0,
    @hayUsuarios BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;

    IF @estado IS NULL OR @estado < 0 OR @estado > 4
        THROW 50001, 'Estado de filtro inválido.', 1;

    SET @nombre = LTRIM(RTRIM(ISNULL(@nombre, N'')));

    SET @hayUsuarios = CONVERT(BIT, CASE WHEN EXISTS
        (SELECT 1 FROM dbo.Usuario) THEN 1 ELSE 0 END);

    SELECT idUsuario, nombre, correoElectronico, rol, estado, bloqueado
    FROM dbo.Usuario
    WHERE (@nombre = N'' OR CHARINDEX(@nombre,
           nombre COLLATE Latin1_General_CI_AI) > 0)
      AND (@estado = 0
           OR (@estado = 1 AND estado = 1)
           OR (@estado = 2 AND estado = 0)
           OR (@estado = 3 AND bloqueado = 1)
           OR (@estado = 4 AND bloqueado = 0))
    ORDER BY nombre, idUsuario;
END;
GO
