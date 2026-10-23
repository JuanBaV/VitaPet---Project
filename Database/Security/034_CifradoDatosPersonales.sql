-- Cifrado de datos personales y sus copias en pagos/facturas. No modifica contraseñas ni DVH/DVV.
-- La clave externa se suministra por sesión desde CifradoDatosDAL, nunca se incorpora al script.
DECLARE @clave nvarchar(128)=CONVERT(nvarchar(128),SESSION_CONTEXT(N'VitaPetClave'));
IF @clave IS NULL THROW 50080,'Falta configurar el archivo de clave de cifrado. Consulte GUIA_CIFRADO.md.',1;
DECLARE @sql nvarchar(max)=N'CREATE SYMMETRIC KEY VitaPet_DatosV1 WITH ALGORITHM=AES_256, KEY_SOURCE='+QUOTENAME(@clave,'''')+
    N', IDENTITY_VALUE=''VitaPet.DatosPersonales.v1'' ENCRYPTION BY PASSWORD='+QUOTENAME(@clave,'''');
EXEC sys.sp_executesql @sql;
SET @sql=N'OPEN SYMMETRIC KEY VitaPet_DatosV1 DECRYPTION BY PASSWORD='+QUOTENAME(@clave,'''');
EXEC sys.sp_executesql @sql;
EXEC sys.sp_set_session_context @key=N'VitaPetClave',@value=NULL;
GO
-- HMAC-SHA256 según RFC 2104. La clave MAC se deriva en .NET con un contexto independiente.
CREATE OR ALTER FUNCTION dbo.fn_HmacDatos(@mensaje varbinary(8000),@clave varbinary(64))
RETURNS varbinary(32)
AS
BEGIN
    IF @clave IS NULL OR @mensaje IS NULL RETURN NULL;
    DECLARE @ipad varbinary(max)=0x,@opad varbinary(max)=0x,@i int=1,@byte int;
    WHILE @i<=64
    BEGIN
        SET @byte=CASE WHEN @i<=DATALENGTH(@clave) THEN CONVERT(int,SUBSTRING(@clave,@i,1)) ELSE 0 END;
        SET @ipad=@ipad+CONVERT(binary(1),@byte ^ 54);
        SET @opad=@opad+CONVERT(binary(1),@byte ^ 92);
        SET @i+=1;
    END;
    RETURN HASHBYTES('SHA2_256',@opad+HASHBYTES('SHA2_256',@ipad+@mensaje));
END;
GO
CREATE OR ALTER FUNCTION dbo.fn_CifrarDato(@texto nvarchar(1000),@campo sysname)
RETURNS varchar(2048)
-- Evita que el optimizador evalúe EncryptByKey dos veces con IV distintos para el MAC y el contenido.
WITH INLINE = OFF
AS
BEGIN
    DECLARE @cifrado varbinary(990)=EncryptByKey(Key_GUID('VitaPet_DatosV1'),@texto,1,@campo);
    DECLARE @mac varbinary(32)=dbo.fn_HmacDatos(CONVERT(varbinary(256),@campo)+@cifrado,CONVERT(varbinary(32),SESSION_CONTEXT(N'VitaPetMAC')));
    RETURN 'VP1:'+CONVERT(varchar(64),@mac,2)+CONVERT(varchar(1980),@cifrado,2);
END;
GO
CREATE OR ALTER FUNCTION dbo.fn_DescifrarDato(@texto varchar(2048),@campo sysname)
RETURNS nvarchar(1000)
AS
BEGIN
    IF LEFT(@texto,4)<>'VP1:' RETURN NULL;
    DECLARE @cifrado varbinary(990)=TRY_CONVERT(varbinary(990),SUBSTRING(@texto,69,1980),2);
    DECLARE @guardado varbinary(32)=TRY_CONVERT(varbinary(32),SUBSTRING(@texto,5,64),2);
    DECLARE @mac varbinary(32)=dbo.fn_HmacDatos(CONVERT(varbinary(256),@campo)+@cifrado,CONVERT(varbinary(32),SESSION_CONTEXT(N'VitaPetMAC')));
    IF @mac IS NULL OR @guardado IS NULL OR @mac<>@guardado RETURN NULL;
    RETURN CONVERT(nvarchar(1000),DecryptByKey(@cifrado,1,@campo));
END;
GO
ALTER TABLE dbo.Usuario DROP CONSTRAINT UQ_Usuario_correoElectronico;
ALTER TABLE dbo.Empresa DROP CONSTRAINT UQ_Empresa_cuit;
ALTER TABLE dbo.Usuario ALTER COLUMN correoElectronico varchar(2048) NOT NULL;
ALTER TABLE dbo.Usuario ALTER COLUMN nombre varchar(2048) NOT NULL;
UPDATE dbo.Usuario SET correoElectronico=dbo.fn_CifrarDato(correoElectronico,N'Usuario.correoElectronico'),nombre=dbo.fn_CifrarDato(nombre,N'Usuario.nombre');
ALTER TABLE dbo.Cliente ALTER COLUMN nombre varchar(2048) NOT NULL;
ALTER TABLE dbo.Cliente ALTER COLUMN apellido varchar(2048) NOT NULL;
ALTER TABLE dbo.Cliente ALTER COLUMN telefono varchar(2048) NOT NULL;
UPDATE dbo.Cliente SET nombre=dbo.fn_CifrarDato(nombre,N'Cliente.nombre'),apellido=dbo.fn_CifrarDato(apellido,N'Cliente.apellido'),telefono=dbo.fn_CifrarDato(telefono,N'Cliente.telefono');
ALTER TABLE dbo.Empresa ALTER COLUMN razonSocial varchar(2048) NOT NULL;
ALTER TABLE dbo.Empresa ALTER COLUMN cuit varchar(2048) NOT NULL;
ALTER TABLE dbo.Empresa ALTER COLUMN direccion varchar(2048) NOT NULL;
ALTER TABLE dbo.Empresa ALTER COLUMN telefono varchar(2048) NOT NULL;
UPDATE dbo.Empresa SET razonSocial=dbo.fn_CifrarDato(razonSocial,N'Empresa.razonSocial'),cuit=dbo.fn_CifrarDato(cuit,N'Empresa.cuit'),direccion=dbo.fn_CifrarDato(direccion,N'Empresa.direccion'),telefono=dbo.fn_CifrarDato(telefono,N'Empresa.telefono');
ALTER TABLE dbo.DetallePago ALTER COLUMN titular nvarchar(2048) NULL;
ALTER TABLE dbo.DetallePago ALTER COLUMN correoFactura varchar(2048) NULL;
UPDATE dbo.DetallePago SET titular=dbo.fn_CifrarDato(titular,N'DetallePago.titular'),correoFactura=dbo.fn_CifrarDato(correoFactura,N'DetallePago.correoFactura');
ALTER TABLE dbo.DetallePago ALTER COLUMN titular varchar(2048) NULL;
ALTER TABLE dbo.Factura ALTER COLUMN clienteNombre nvarchar(2048) NOT NULL;
ALTER TABLE dbo.Factura ALTER COLUMN clienteCorreo varchar(2048) NOT NULL;
ALTER TABLE dbo.Factura ALTER COLUMN titular nvarchar(2048) NULL;
UPDATE dbo.Factura SET clienteNombre=dbo.fn_CifrarDato(clienteNombre,N'Factura.clienteNombre'),clienteCorreo=dbo.fn_CifrarDato(clienteCorreo,N'Factura.clienteCorreo'),titular=dbo.fn_CifrarDato(titular,N'Factura.titular');
ALTER TABLE dbo.Factura ALTER COLUMN clienteNombre varchar(2048) NOT NULL;
ALTER TABLE dbo.Factura ALTER COLUMN titular varchar(2048) NULL;
-- Las búsquedas de duplicados mantienen UPDLOCK/HOLDLOCK sobre vistas de una sola tabla.
-- El índice físico también rechaza un ciphertext idéntico, aunque AES usa IV aleatorio.
ALTER TABLE dbo.Usuario ADD CONSTRAINT UQ_Usuario_correoElectronico UNIQUE(correoElectronico);
ALTER TABLE dbo.Empresa ADD CONSTRAINT UQ_Empresa_cuit UNIQUE(cuit);
GO
CREATE OR ALTER VIEW dbo.vw_UsuarioDescifrado AS SELECT
    [idUsuario],
    CONVERT(varchar(100),dbo.fn_DescifrarDato([correoElectronico],N'Usuario.correoElectronico')) AS [correoElectronico],
    [contrasena],
    CONVERT(varchar(100),dbo.fn_DescifrarDato([nombre],N'Usuario.nombre')) AS [nombre],
    [estado],
    [intentosFallidos],
    [bloqueado],
    [idioma],
    [dvh],
    [requiereRestablecerContrasena],
    [idRol]
FROM dbo.Usuario;
GO
CREATE OR ALTER VIEW dbo.vw_ClienteDescifrado AS SELECT
    [idCliente],
    [idUsuario],
    [idEmpresa],
    CONVERT(varchar(60),dbo.fn_DescifrarDato([nombre],N'Cliente.nombre')) AS [nombre],
    CONVERT(varchar(60),dbo.fn_DescifrarDato([apellido],N'Cliente.apellido')) AS [apellido],
    CONVERT(varchar(30),dbo.fn_DescifrarDato([telefono],N'Cliente.telefono')) AS [telefono],
    [tipoCliente],
    [estado],
    [dvh]
FROM dbo.Cliente;
GO
CREATE OR ALTER VIEW dbo.vw_EmpresaDescifrado AS SELECT
    [idEmpresa],
    CONVERT(varchar(100),dbo.fn_DescifrarDato([razonSocial],N'Empresa.razonSocial')) AS [razonSocial],
    CONVERT(varchar(20),dbo.fn_DescifrarDato([cuit],N'Empresa.cuit')) AS [cuit],
    CONVERT(varchar(150),dbo.fn_DescifrarDato([direccion],N'Empresa.direccion')) AS [direccion],
    CONVERT(varchar(30),dbo.fn_DescifrarDato([telefono],N'Empresa.telefono')) AS [telefono],
    [estado],
    [dvh]
FROM dbo.Empresa;
GO
CREATE OR ALTER VIEW dbo.vw_DetallePagoDescifrado AS SELECT
    [idPago],
    [precioMascota],
    [precioMascotaAdicional],
    [cantidadMascotas],
    CONVERT(nvarchar(100),dbo.fn_DescifrarDato([titular],N'DetallePago.titular')) AS [titular],
    [dvh],
    CONVERT(varchar(100),dbo.fn_DescifrarDato([correoFactura],N'DetallePago.correoFactura')) AS [correoFactura]
FROM dbo.DetallePago;
GO
CREATE OR ALTER VIEW dbo.vw_FacturaDescifrado AS SELECT
    [idFactura],
    [numeroFactura],
    [idPago],
    [idCliente],
    [idSuscripcion],
    [idPlan],
    CONVERT(nvarchar(121),dbo.fn_DescifrarDato([clienteNombre],N'Factura.clienteNombre')) AS [clienteNombre],
    CONVERT(varchar(100),dbo.fn_DescifrarDato([clienteCorreo],N'Factura.clienteCorreo')) AS [clienteCorreo],
    [planNombre],
    [concepto],
    [periodoDesde],
    [periodoHasta],
    [fechaEmision],
    [precioMascota],
    [precioMascotaAdicional],
    [cantidadMascotas],
    CONVERT(nvarchar(100),dbo.fn_DescifrarDato([titular],N'Factura.titular')) AS [titular],
    [importe],
    [moneda],
    [medioPago],
    [estado],
    [dvh]
FROM dbo.Factura;
GO
-- Reutiliza las validaciones, filtros y permisos vigentes; sólo cambia el origen de lectura.
DECLARE @nombre sysname,@def nvarchar(max),@antes nvarchar(max);
DECLARE procedimientos CURSOR LOCAL FAST_FORWARD FOR
    SELECT o.name,m.definition FROM sys.sql_modules m JOIN sys.objects o ON o.object_id=m.object_id WHERE o.type='P' AND o.schema_id=SCHEMA_ID('dbo');
OPEN procedimientos; FETCH NEXT FROM procedimientos INTO @nombre,@def;
WHILE @@FETCH_STATUS=0
BEGIN
    SET @antes=@def;
    SET @def=REPLACE(@def,N'FROM dbo.Usuario ',N'FROM dbo.vw_UsuarioDescifrado ');
    SET @def=REPLACE(@def,N'FROM dbo.Usuario'+CHAR(13),N'FROM dbo.vw_UsuarioDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'FROM dbo.Usuario'+CHAR(10),N'FROM dbo.vw_UsuarioDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'FROM dbo.Usuario)',N'FROM dbo.vw_UsuarioDescifrado)');
    SET @def=REPLACE(@def,N'FROM dbo.Usuario;',N'FROM dbo.vw_UsuarioDescifrado;');
    SET @def=REPLACE(@def,N'JOIN dbo.Usuario ',N'JOIN dbo.vw_UsuarioDescifrado ');
    SET @def=REPLACE(@def,N'JOIN dbo.Usuario'+CHAR(13),N'JOIN dbo.vw_UsuarioDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'JOIN dbo.Usuario'+CHAR(10),N'JOIN dbo.vw_UsuarioDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'JOIN dbo.Usuario)',N'JOIN dbo.vw_UsuarioDescifrado)');
    SET @def=REPLACE(@def,N'JOIN dbo.Usuario;',N'JOIN dbo.vw_UsuarioDescifrado;');
    SET @def=REPLACE(@def,N'FROM dbo.Cliente ',N'FROM dbo.vw_ClienteDescifrado ');
    SET @def=REPLACE(@def,N'FROM dbo.Cliente'+CHAR(13),N'FROM dbo.vw_ClienteDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'FROM dbo.Cliente'+CHAR(10),N'FROM dbo.vw_ClienteDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'FROM dbo.Cliente)',N'FROM dbo.vw_ClienteDescifrado)');
    SET @def=REPLACE(@def,N'FROM dbo.Cliente;',N'FROM dbo.vw_ClienteDescifrado;');
    SET @def=REPLACE(@def,N'JOIN dbo.Cliente ',N'JOIN dbo.vw_ClienteDescifrado ');
    SET @def=REPLACE(@def,N'JOIN dbo.Cliente'+CHAR(13),N'JOIN dbo.vw_ClienteDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'JOIN dbo.Cliente'+CHAR(10),N'JOIN dbo.vw_ClienteDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'JOIN dbo.Cliente)',N'JOIN dbo.vw_ClienteDescifrado)');
    SET @def=REPLACE(@def,N'JOIN dbo.Cliente;',N'JOIN dbo.vw_ClienteDescifrado;');
    SET @def=REPLACE(@def,N'FROM dbo.Empresa ',N'FROM dbo.vw_EmpresaDescifrado ');
    SET @def=REPLACE(@def,N'FROM dbo.Empresa'+CHAR(13),N'FROM dbo.vw_EmpresaDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'FROM dbo.Empresa'+CHAR(10),N'FROM dbo.vw_EmpresaDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'FROM dbo.Empresa)',N'FROM dbo.vw_EmpresaDescifrado)');
    SET @def=REPLACE(@def,N'FROM dbo.Empresa;',N'FROM dbo.vw_EmpresaDescifrado;');
    SET @def=REPLACE(@def,N'JOIN dbo.Empresa ',N'JOIN dbo.vw_EmpresaDescifrado ');
    SET @def=REPLACE(@def,N'JOIN dbo.Empresa'+CHAR(13),N'JOIN dbo.vw_EmpresaDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'JOIN dbo.Empresa'+CHAR(10),N'JOIN dbo.vw_EmpresaDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'JOIN dbo.Empresa)',N'JOIN dbo.vw_EmpresaDescifrado)');
    SET @def=REPLACE(@def,N'JOIN dbo.Empresa;',N'JOIN dbo.vw_EmpresaDescifrado;');
    SET @def=REPLACE(@def,N'FROM dbo.DetallePago ',N'FROM dbo.vw_DetallePagoDescifrado ');
    SET @def=REPLACE(@def,N'FROM dbo.DetallePago'+CHAR(13),N'FROM dbo.vw_DetallePagoDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'FROM dbo.DetallePago'+CHAR(10),N'FROM dbo.vw_DetallePagoDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'FROM dbo.DetallePago)',N'FROM dbo.vw_DetallePagoDescifrado)');
    SET @def=REPLACE(@def,N'FROM dbo.DetallePago;',N'FROM dbo.vw_DetallePagoDescifrado;');
    SET @def=REPLACE(@def,N'JOIN dbo.DetallePago ',N'JOIN dbo.vw_DetallePagoDescifrado ');
    SET @def=REPLACE(@def,N'JOIN dbo.DetallePago'+CHAR(13),N'JOIN dbo.vw_DetallePagoDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'JOIN dbo.DetallePago'+CHAR(10),N'JOIN dbo.vw_DetallePagoDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'JOIN dbo.DetallePago)',N'JOIN dbo.vw_DetallePagoDescifrado)');
    SET @def=REPLACE(@def,N'JOIN dbo.DetallePago;',N'JOIN dbo.vw_DetallePagoDescifrado;');
    SET @def=REPLACE(@def,N'FROM dbo.Factura ',N'FROM dbo.vw_FacturaDescifrado ');
    SET @def=REPLACE(@def,N'FROM dbo.Factura'+CHAR(13),N'FROM dbo.vw_FacturaDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'FROM dbo.Factura'+CHAR(10),N'FROM dbo.vw_FacturaDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'FROM dbo.Factura)',N'FROM dbo.vw_FacturaDescifrado)');
    SET @def=REPLACE(@def,N'FROM dbo.Factura;',N'FROM dbo.vw_FacturaDescifrado;');
    SET @def=REPLACE(@def,N'JOIN dbo.Factura ',N'JOIN dbo.vw_FacturaDescifrado ');
    SET @def=REPLACE(@def,N'JOIN dbo.Factura'+CHAR(13),N'JOIN dbo.vw_FacturaDescifrado'+CHAR(13));
    SET @def=REPLACE(@def,N'JOIN dbo.Factura'+CHAR(10),N'JOIN dbo.vw_FacturaDescifrado'+CHAR(10));
    SET @def=REPLACE(@def,N'JOIN dbo.Factura)',N'JOIN dbo.vw_FacturaDescifrado)');
    SET @def=REPLACE(@def,N'JOIN dbo.Factura;',N'JOIN dbo.vw_FacturaDescifrado;');
    IF @antes<>@def
    BEGIN
        SET @def=REPLACE(REPLACE(REPLACE(@def,N'CREATE   PROCEDURE',N'CREATE OR ALTER PROCEDURE'),N'CREATE  PROCEDURE',N'CREATE OR ALTER PROCEDURE'),N'CREATE PROCEDURE',N'CREATE OR ALTER PROCEDURE');
        EXEC sys.sp_executesql @def;
    END;
    FETCH NEXT FROM procedimientos INTO @nombre,@def;
END;
CLOSE procedimientos; DEALLOCATE procedimientos;
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_Cliente_Registrar]
    @idUsuario INT, @nombre VARCHAR(60), @apellido VARCHAR(60), @telefono VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT = 0 THROW 50049, 'Se requiere una transaccion de integridad.', 1;
    INSERT dbo.Cliente(idUsuario, idEmpresa, nombre, apellido, telefono, tipoCliente, estado, dvh)
    OUTPUT INSERTED.idCliente
    VALUES(@idUsuario, NULL, dbo.fn_CifrarDato(@nombre,N'Cliente.nombre'), dbo.fn_CifrarDato(@apellido,N'Cliente.apellido'), dbo.fn_CifrarDato(@telefono,N'Cliente.telefono'), 'Individual', 1, '');
END;
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_ClienteCorporativo_RegistrarCliente]
    @idAdministrador INT,@idUsuario INT,@idEmpresa INT,@nombre NVARCHAR(MAX),@apellido NVARCHAR(MAX),@telefono NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idAdministrador AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_EmpresaDescifrado WHERE idEmpresa=@idEmpresa) THROW 50011,'No existen datos asociados.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_EmpresaDescifrado WHERE idEmpresa=@idEmpresa AND estado=1) THROW 50072,'La empresa seleccionada se encuentra inactiva.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND idRol=(SELECT idComponente FROM dbo.PermisoComponente WHERE nombre='Cliente' AND tipo='Perfil' AND sistema=1) AND estado=1 AND requiereRestablecerContrasena=1)
       OR EXISTS(SELECT 1 FROM dbo.vw_ClienteDescifrado WHERE idUsuario=@idUsuario) THROW 50049,'Cuenta no disponible para el alta corporativa.',1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@apellido)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@telefono)),N'') IS NULL THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>60 OR LEN(@apellido)>60 OR LEN(@nombre+N' '+@apellido)>100 OR LEN(@telefono)>30 THROW 50025,'Datos invalidos.',1;
    INSERT dbo.Cliente(idUsuario,idEmpresa,nombre,apellido,telefono,tipoCliente,estado,dvh)
    OUTPUT inserted.idCliente VALUES(@idUsuario,@idEmpresa,dbo.fn_CifrarDato(@nombre,N'Cliente.nombre'),dbo.fn_CifrarDato(@apellido,N'Cliente.apellido'),dbo.fn_CifrarDato(@telefono,N'Cliente.telefono'),'Corporativo',1,'');
END;
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_ClienteCorporativo_RegistrarUsuario]
    @idAdministrador INT,@nombre NVARCHAR(MAX),@correo NVARCHAR(MAX),@hash VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idAdministrador AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@correo)),N'') IS NULL THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>100 OR LEN(@correo)>100 OR LEN(@hash)<>64 OR @hash IS NULL OR @hash COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%'
       OR @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
       OR @correo COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@correo)) COLLATE Latin1_General_100_BIN2
        THROW 50025,'Datos invalidos.',1;
    IF EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WITH(UPDLOCK,HOLDLOCK) WHERE correoElectronico=@correo) THROW 50001,'Correo ya registrado.',1;
    BEGIN TRY
        INSERT dbo.Usuario(nombre,correoElectronico,contrasena,idRol,estado,intentosFallidos,bloqueado,idioma,requiereRestablecerContrasena,dvh)
        OUTPUT inserted.idUsuario VALUES(dbo.fn_CifrarDato(@nombre,N'Usuario.nombre'),dbo.fn_CifrarDato(@correo,N'Usuario.correoElectronico'),@hash,(SELECT idRol FROM dbo.Rol r JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol WHERE p.nombre='Cliente' AND p.sistema=1),1,0,0,'es',1,'');
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50001,'Correo ya registrado.',1;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_Empresa_Actualizar] @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX),@idEmpresa INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'ModificarEmpresa')=1 AND estado=1 AND bloqueado=0)
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
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_EmpresaDescifrado WITH(UPDLOCK,HOLDLOCK) WHERE idEmpresa=@idEmpresa)
        THROW 50011, 'No existen datos asociados.', 1;
    IF EXISTS(SELECT 1 FROM dbo.vw_EmpresaDescifrado WITH(UPDLOCK,HOLDLOCK) WHERE REPLACE(cuit,'-','')=@cuit AND idEmpresa<>@idEmpresa)
        THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
    BEGIN TRY
        -- Conserva identificador, estado y asociaciones. Sin comparacion de versiones.
        UPDATE dbo.Empresa SET razonSocial=dbo.fn_CifrarDato(@razonSocial,N'Empresa.razonSocial'),cuit=dbo.fn_CifrarDato(@cuit,N'Empresa.cuit'),direccion=dbo.fn_CifrarDato(@direccion,N'Empresa.direccion'),telefono=dbo.fn_CifrarDato(@telefono,N'Empresa.telefono') WHERE idEmpresa=@idEmpresa;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_Empresa_Registrar] @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaEmpresa')=1 AND estado=1 AND bloqueado=0)
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
    IF EXISTS(SELECT 1 FROM dbo.vw_EmpresaDescifrado WITH(UPDLOCK,HOLDLOCK) WHERE REPLACE(cuit,'-','')=@cuit)
        THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
    BEGIN TRY
        INSERT dbo.Empresa(razonSocial,cuit,direccion,telefono,estado,dvh)
        OUTPUT inserted.idEmpresa VALUES(dbo.fn_CifrarDato(@razonSocial,N'Empresa.razonSocial'),dbo.fn_CifrarDato(@cuit,N'Empresa.cuit'),dbo.fn_CifrarDato(@direccion,N'Empresa.direccion'),dbo.fn_CifrarDato(@telefono,N'Empresa.telefono'),1,'');
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50068, 'El CUIT ya se encuentra registrado para otra empresa.', 1;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Factura_Emitir @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50080,'Se requiere una transaccion de integridad.',1;
    DECLARE @pago INT=(SELECT TOP(1) p.idPago FROM dbo.Pago p WITH(UPDLOCK,HOLDLOCK)
    INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=s.idCliente
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND s.idSuscripcion=@idSuscripcion AND p.estado='Aprobado' ORDER BY p.idPago);
    IF @pago IS NULL THROW 50054,'No existe un pago aprobado propio.',1;
    DECLARE @factura INT=(SELECT idFactura FROM dbo.vw_FacturaDescifrado WITH(UPDLOCK,HOLDLOCK) WHERE idPago=@pago);
    IF @factura IS NOT NULL
    BEGIN SELECT @factura id,CONVERT(BIT,0) nuevo;RETURN;END;
    DECLARE @numero BIGINT=(SELECT ISNULL(MAX(CONVERT(BIGINT,RIGHT(numeroFactura,10))),0)+1 FROM dbo.vw_FacturaDescifrado WITH(UPDLOCK,HOLDLOCK));
    IF @numero>9999999999 THROW 50080,'Numeracion agotada.',1;
    INSERT dbo.Factura(numeroFactura,idPago,idCliente,idSuscripcion,idPlan,clienteNombre,clienteCorreo,planNombre,concepto,periodoDesde,periodoHasta,fechaEmision,precioMascota,precioMascotaAdicional,cantidadMascotas,titular,importe,moneda,medioPago,estado,dvh)
    SELECT '0001-'+RIGHT('0000000000'+CONVERT(VARCHAR(10),@numero),10),p.idPago,c.idCliente,s.idSuscripcion,s.idPlan,dbo.fn_CifrarDato(c.nombre+' '+c.apellido,N'Factura.clienteNombre'),dbo.fn_CifrarDato(u.correoElectronico,N'Factura.clienteCorreo'),pl.nombre,
        N'Suscripción mensual - '+pl.nombre,p.fechaPago,DATEADD(DAY,-1,DATEADD(MONTH,1,p.fechaPago)),SYSDATETIME(),d.precioMascota,d.precioMascotaAdicional,d.cantidadMascotas,dbo.fn_CifrarDato(d.titular,N'Factura.titular'),p.importe,'USD',p.medioPago,'Emitida',''
    FROM dbo.Pago p INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=s.idCliente
    INNER JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario INNER JOIN dbo.[Plan] pl ON pl.idPlan=s.idPlan LEFT JOIN dbo.vw_DetallePagoDescifrado d ON d.idPago=p.idPago WHERE p.idPago=@pago;
    IF @@ROWCOUNT<>1 THROW 50079,'No se pudo preparar la factura.',1;
    SELECT CONVERT(INT,SCOPE_IDENTITY()) id,CONVERT(BIT,1) nuevo;
END;
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
    SELECT @pago,precio,ROUND(precio*0.8,2),cantidadMascotas,dbo.fn_CifrarDato(@titular,N'DetallePago.titular'),dbo.fn_CifrarDato(@correoFactura,N'DetallePago.correoFactura'),'' FROM @resumen;
    SELECT @pago;
END;
GO
CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_GuardarAdministracion]
    @idUsuario INT,
    @version VARCHAR(64),
    @nombre VARCHAR(100),
    @correo VARCHAR(100),
    @idRol INT,
    @estado BIT,
    @bloqueado BIT,
    @intentos TINYINT,
    @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @@TRANCOUNT = 0
        THROW 50039, 'Se requiere una transacción.', 1;

    -- Leer una sola vez y mantener el usuario bloqueado hasta el commit.
    DECLARE @versionActual VARCHAR(64);
    SELECT @versionActual = dvh
    FROM dbo.vw_UsuarioDescifrado WITH (UPDLOCK, HOLDLOCK)
    WHERE idUsuario = @idUsuario;

    IF @@ROWCOUNT = 0
        THROW 50036, 'El usuario seleccionado ya no existe.', 1;

    IF @version IS NULL OR @versionActual IS NULL OR @versionActual <> @version
        THROW 50038, 'Los datos del usuario cambiaron desde su consulta.', 1;

    IF EXISTS (SELECT 1 FROM dbo.vw_UsuarioDescifrado WITH (UPDLOCK, HOLDLOCK)
               WHERE correoElectronico = @correo AND idUsuario <> @idUsuario)
        THROW 50001, 'Correo ya registrado.', 1;

    UPDATE dbo.Usuario
    SET nombre = dbo.fn_CifrarDato(@nombre,N'Usuario.nombre'),
        correoElectronico = dbo.fn_CifrarDato(@correo,N'Usuario.correoElectronico'),
        idRol = @idRol,
        estado = @estado,
        bloqueado = @bloqueado,
        intentosFallidos = @intentos,
        dvh = @dvh
    WHERE idUsuario = @idUsuario AND dvh = @version;

    IF @@ROWCOUNT <> 1
        THROW 50039, 'No se pudo actualizar el usuario.', 1;
END;
GO
-- la integridad; una migración SQL no debe insertar filas sin sus referencias.
CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_RegistrarCliente]
    @nombre VARCHAR(100), @correo VARCHAR(100), @contrasena VARCHAR(255), @idRol INT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT = 0 THROW 50049, 'Se requiere una transaccion de integridad.', 1;
    IF EXISTS (SELECT 1 FROM dbo.vw_UsuarioDescifrado WITH (UPDLOCK, HOLDLOCK) WHERE correoElectronico=@correo)
        THROW 50001, 'Correo ya registrado.', 1;
    INSERT dbo.Usuario(nombre, correoElectronico, contrasena, idRol, estado,
        intentosFallidos, bloqueado, idioma, dvh)
    OUTPUT INSERTED.idUsuario
    VALUES(dbo.fn_CifrarDato(@nombre,N'Usuario.nombre'), dbo.fn_CifrarDato(@correo,N'Usuario.correoElectronico'), @contrasena, @idRol, 1, 0, 0, 'es', '');
END;
GO
