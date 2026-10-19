-- CU-005-001 a CU-005-004. Reutiliza Empresa y su clave unica CUIT existentes.
-- La BLL guarda DVH, DVV, referencias y auditoria dentro de la transaccion ambiente.
CREATE OR ALTER PROCEDURE dbo.sp_Empresa_Listar @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    SELECT idEmpresa,razonSocial,cuit,direccion,telefono,estado FROM dbo.Empresa ORDER BY razonSocial,idEmpresa;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Empresa_Consultar @idUsuario INT,@idEmpresa INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empresa WHERE idEmpresa=@idEmpresa) THROW 50011, 'No existen datos asociados.', 1;
    SELECT idEmpresa,razonSocial,cuit,direccion,telefono,estado FROM dbo.Empresa WHERE idEmpresa=@idEmpresa;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Empresa_CuitRegistrado @idUsuario INT,@cuit VARCHAR(20),@idEmpresa INT=0
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    IF @cuit IS NULL OR LEN(@cuit)<>11 OR @cuit COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' THROW 50025, 'Datos invalidos.', 1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.Empresa WITH(UPDLOCK,HOLDLOCK) WHERE REPLACE(cuit,'-','')=@cuit AND idEmpresa<>@idEmpresa) THEN 1 ELSE 0 END);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Empresa_Registrar @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50069, 'Se requiere una transaccion de integridad.', 1;
    IF NULLIF(LTRIM(RTRIM(@razonSocial)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@cuit)),N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@direccion)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@telefono)),N'') IS NULL
        THROW 50002, 'Datos incompletos.', 1;
    SELECT @razonSocial=LTRIM(RTRIM(@razonSocial)),@cuit=LTRIM(RTRIM(@cuit)),@direccion=LTRIM(RTRIM(@direccion)),@telefono=LTRIM(RTRIM(@telefono));
    IF LEN(@razonSocial)>100 OR LEN(@cuit)>20 OR LEN(@direccion)>150 OR LEN(@telefono)>30
        THROW 50025, 'Datos invalidos.', 1;
    IF NOT(LEN(@cuit)=11 OR (LEN(@cuit)=13 AND SUBSTRING(@cuit,3,1)=N'-' AND SUBSTRING(@cuit,12,1)=N'-'))
        THROW 50025, 'Datos invalidos.', 1;
    SET @cuit=REPLACE(@cuit,N'-',N'');
    IF LEN(@cuit)<>11 OR @cuit COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9 +()-]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[0-9]%'
        THROW 50025, 'Datos invalidos.', 1;
    IF @razonSocial COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@razonSocial)) COLLATE Latin1_General_100_BIN2
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
CREATE OR ALTER PROCEDURE dbo.sp_Empresa_Actualizar @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX),@idEmpresa INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND rol='Administrador' AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50070, 'Se requiere una transaccion de integridad.', 1;
    IF NULLIF(LTRIM(RTRIM(@razonSocial)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@cuit)),N'') IS NULL
       OR NULLIF(LTRIM(RTRIM(@direccion)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@telefono)),N'') IS NULL
        THROW 50002, 'Datos incompletos.', 1;
    SELECT @razonSocial=LTRIM(RTRIM(@razonSocial)),@cuit=LTRIM(RTRIM(@cuit)),@direccion=LTRIM(RTRIM(@direccion)),@telefono=LTRIM(RTRIM(@telefono));
    IF LEN(@razonSocial)>100 OR LEN(@cuit)>20 OR LEN(@direccion)>150 OR LEN(@telefono)>30
        THROW 50025, 'Datos invalidos.', 1;
    IF NOT(LEN(@cuit)=11 OR (LEN(@cuit)=13 AND SUBSTRING(@cuit,3,1)=N'-' AND SUBSTRING(@cuit,12,1)=N'-'))
        THROW 50025, 'Datos invalidos.', 1;
    SET @cuit=REPLACE(@cuit,N'-',N'');
    IF LEN(@cuit)<>11 OR @cuit COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 LIKE N'%[^0-9 +()-]%'
       OR @telefono COLLATE Latin1_General_100_BIN2 NOT LIKE N'%[0-9]%'
        THROW 50025, 'Datos invalidos.', 1;
    IF @razonSocial COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@razonSocial)) COLLATE Latin1_General_100_BIN2
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
