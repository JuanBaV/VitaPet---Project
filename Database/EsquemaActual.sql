-- Esquema cifrado vigente de VitaPet, sin datos de usuarios ni conversiones historicas.
-- Creacion exclusiva en una base nueva de recuperacion; clave suministrada por sesion.
DECLARE @clave nvarchar(128)=CONVERT(nvarchar(128),SESSION_CONTEXT(N'VitaPetClave'));
IF @clave IS NULL THROW 50080,'Falta configurar el archivo con la clave de cifrado original.',1;
DECLARE @sql nvarchar(max)=N'CREATE SYMMETRIC KEY VitaPet_DatosV1 WITH ALGORITHM=AES_256, KEY_SOURCE='+QUOTENAME(@clave,'''')+
    N', IDENTITY_VALUE=''VitaPet.DatosPersonales.v1'' ENCRYPTION BY PASSWORD='+QUOTENAME(@clave,'''');
EXEC sys.sp_executesql @sql;
SET @sql=N'OPEN SYMMETRIC KEY VitaPet_DatosV1 DECRYPTION BY PASSWORD='+QUOTENAME(@clave,'''');
EXEC sys.sp_executesql @sql;
EXEC sys.sp_set_session_context @key=N'VitaPetClave',@value=NULL;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION dbo.fn_HmacDatos(@mensaje varbinary(8000),@clave varbinary(64))
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION dbo.fn_DescifrarDato(@texto varchar(2048),@campo sysname)
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION dbo.fn_CifrarDato(@texto nvarchar(1000),@campo sysname)
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   FUNCTION dbo.fn_RolPermite(@idRol INT,@pantallas VARCHAR(1000))
RETURNS BIT
AS
BEGIN
    DECLARE @permitido BIT=0;
    WITH componentes AS (
        SELECT c.idComponente,c.pantalla FROM dbo.Rol r JOIN dbo.PermisoComponente c ON c.idComponente=r.idRol WHERE r.idRol=@idRol
        UNION ALL
        SELECT hijo.idComponente,hijo.pantalla FROM componentes padre
        JOIN dbo.RolPermiso r ON r.idRol=padre.idComponente
        JOIN dbo.PermisoComponente hijo ON hijo.idComponente=r.idComponente
    )
    SELECT TOP(1) @permitido=1 FROM componentes
    WHERE CHARINDEX(','+pantalla+',',','+@pantallas+',')>0;
    RETURN @permitido;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Usuario](
	[idUsuario] [int] IDENTITY(1,1) NOT NULL,
	[correoElectronico] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[contrasena] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[nombre] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[estado] [bit] NOT NULL,
	[intentosFallidos] [tinyint] NOT NULL,
	[bloqueado] [bit] NOT NULL,
	[idioma] [varchar](5) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[requiereRestablecerContrasena] [bit] NOT NULL,
	[idRol] [int] NOT NULL,
 CONSTRAINT [PK_Usuario] PRIMARY KEY CLUSTERED
(
	[idUsuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Usuario_correoElectronico] UNIQUE NONCLUSTERED
(
	[correoElectronico] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW dbo.vw_UsuarioDescifrado AS SELECT
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cliente](
	[idCliente] [int] IDENTITY(1,1) NOT NULL,
	[idUsuario] [int] NOT NULL,
	[idEmpresa] [int] NULL,
	[nombre] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[apellido] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[telefono] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[tipoCliente] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[estado] [bit] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Cliente] PRIMARY KEY CLUSTERED
(
	[idCliente] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Cliente_idUsuario] UNIQUE NONCLUSTERED
(
	[idUsuario] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW dbo.vw_ClienteDescifrado AS SELECT
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Empresa](
	[idEmpresa] [int] IDENTITY(1,1) NOT NULL,
	[razonSocial] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[cuit] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[direccion] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[telefono] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[estado] [bit] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Empresa] PRIMARY KEY CLUSTERED
(
	[idEmpresa] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Empresa_cuit] UNIQUE NONCLUSTERED
(
	[cuit] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW dbo.vw_EmpresaDescifrado AS SELECT
    [idEmpresa],
    CONVERT(varchar(100),dbo.fn_DescifrarDato([razonSocial],N'Empresa.razonSocial')) AS [razonSocial],
    CONVERT(varchar(20),dbo.fn_DescifrarDato([cuit],N'Empresa.cuit')) AS [cuit],
    CONVERT(varchar(150),dbo.fn_DescifrarDato([direccion],N'Empresa.direccion')) AS [direccion],
    CONVERT(varchar(30),dbo.fn_DescifrarDato([telefono],N'Empresa.telefono')) AS [telefono],
    [estado],
    [dvh]
FROM dbo.Empresa;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DetallePago](
	[idPago] [int] NOT NULL,
	[precioMascota] [decimal](10, 2) NOT NULL,
	[precioMascotaAdicional] [decimal](10, 2) NOT NULL,
	[cantidadMascotas] [int] NOT NULL,
	[titular] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[correoFactura] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
PRIMARY KEY CLUSTERED
(
	[idPago] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW dbo.vw_DetallePagoDescifrado AS SELECT
    [idPago],
    [precioMascota],
    [precioMascotaAdicional],
    [cantidadMascotas],
    CONVERT(nvarchar(100),dbo.fn_DescifrarDato([titular],N'DetallePago.titular')) AS [titular],
    [dvh],
    CONVERT(varchar(100),dbo.fn_DescifrarDato([correoFactura],N'DetallePago.correoFactura')) AS [correoFactura]
FROM dbo.DetallePago;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Factura](
	[idFactura] [int] IDENTITY(1,1) NOT NULL,
	[numeroFactura] [varchar](15) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[idPago] [int] NOT NULL,
	[idCliente] [int] NOT NULL,
	[idSuscripcion] [int] NOT NULL,
	[idPlan] [int] NOT NULL,
	[clienteNombre] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[clienteCorreo] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[planNombre] [varchar](80) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[concepto] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[periodoDesde] [date] NOT NULL,
	[periodoHasta] [date] NOT NULL,
	[fechaEmision] [datetime2](7) NOT NULL,
	[precioMascota] [decimal](10, 2) NULL,
	[precioMascotaAdicional] [decimal](10, 2) NULL,
	[cantidadMascotas] [int] NULL,
	[titular] [varchar](2048) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[importe] [decimal](10, 2) NOT NULL,
	[moneda] [char](3) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[medioPago] [varchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[estado] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED
(
	[idFactura] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED
(
	[numeroFactura] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED
(
	[idPago] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   VIEW dbo.vw_FacturaDescifrado AS SELECT
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ReferenciaIntegridad](
	[tabla] [varchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[fila] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[columna] [varchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[huella] [char](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_ReferenciaIntegridad] PRIMARY KEY CLUSTERED
(
	[tabla] ASC,
	[fila] ASC,
	[columna] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Plan](
	[idPlan] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](80) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[descripcion] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[precio] [decimal](10, 2) NULL,
	[estado] [bit] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[esCorporativo] [bit] NOT NULL,
 CONSTRAINT [PK_Plan] PRIMARY KEY CLUSTERED
(
	[idPlan] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_Plan_nombre] UNIQUE NONCLUSTERED
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[PermisoComponente](
	[idComponente] [int] IDENTITY(1,1) NOT NULL,
	[nombre] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[tipo] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[pantalla] [varchar](80) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[sistema] [bit] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
PRIMARY KEY CLUSTERED
(
	[idComponente] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
UNIQUE NONCLUSTERED
(
	[nombre] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AgendaTurno](
	[idAgendaTurno] [int] IDENTITY(1,1) NOT NULL,
	[zona] [varchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[prestacion] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[horaInicio] [time](0) NOT NULL,
	[horaFin] [time](0) NOT NULL,
	[duracionMinutos] [smallint] NOT NULL,
	[horizonteDias] [smallint] NOT NULL,
	[estado] [bit] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_AgendaTurno] PRIMARY KEY CLUSTERED
(
	[idAgendaTurno] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_AgendaTurno_ZonaPrestacion] UNIQUE NONCLUSTERED
(
	[zona] ASC,
	[prestacion] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__SchemaMigrations](
	[migrationId] [nvarchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[checksum] [char](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[appliedUtc] [datetime2](0) NOT NULL,
 CONSTRAINT [PK_SchemaMigrations] PRIMARY KEY CLUSTERED
(
	[migrationId] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[__FormatoIntegridad](
	[id] [tinyint] NOT NULL,
	[formato] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_FormatoIntegridad] PRIMARY KEY CLUSTERED
(
	[id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ErrorSistema](
	[codigo] [varchar](10) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[descripcion] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[criticidad] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_ErrorSistema] PRIMARY KEY CLUSTERED
(
	[codigo] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DVV](
	[idDVV] [int] IDENTITY(1,1) NOT NULL,
	[nombreTabla] [varchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[valorDVV] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_DVV] PRIMARY KEY CLUSTERED
(
	[idDVV] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_DVV_nombreTabla] UNIQUE NONCLUSTERED
(
	[nombreTabla] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Cobertura](
	[idCobertura] [int] IDENTITY(1,1) NOT NULL,
	[idPlan] [int] NOT NULL,
	[prestacion] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[detalle] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[estado] [bit] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Cobertura] PRIMARY KEY CLUSTERED
(
	[idCobertura] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Rol](
	[idRol] [int] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Rol] PRIMARY KEY CLUSTERED
(
	[idRol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RolPermiso](
	[idRolPermiso] [int] IDENTITY(1,1) NOT NULL,
	[idRol] [int] NOT NULL,
	[idComponente] [int] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_RolPermiso] PRIMARY KEY CLUSTERED
(
	[idRolPermiso] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY],
 CONSTRAINT [UQ_RolPermiso] UNIQUE NONCLUSTERED
(
	[idRol] ASC,
	[idComponente] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Bitacora](
	[idBitacora] [int] IDENTITY(1,1) NOT NULL,
	[idUsuario] [int] NULL,
	[fecha] [datetime] NOT NULL,
	[accion] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[modulo] [varchar](50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[criticidad] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[archivado] [bit] NOT NULL,
 CONSTRAINT [PK_Bitacora] PRIMARY KEY CLUSTERED
(
	[idBitacora] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Mascota](
	[idMascota] [int] IDENTITY(1,1) NOT NULL,
	[idCliente] [int] NOT NULL,
	[nombre] [varchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[especie] [varchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[raza] [varchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[fechaNacimiento] [date] NOT NULL,
	[estado] [bit] NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Mascota] PRIMARY KEY CLUSTERED
(
	[idMascota] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Suscripcion](
	[idSuscripcion] [int] IDENTITY(1,1) NOT NULL,
	[idCliente] [int] NOT NULL,
	[idPlan] [int] NOT NULL,
	[fechaAlta] [date] NOT NULL,
	[fechaVencimiento] [date] NOT NULL,
	[estado] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Suscripcion] PRIMARY KEY CLUSTERED
(
	[idSuscripcion] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Turno](
	[idTurno] [int] IDENTITY(1,1) NOT NULL,
	[idMascota] [int] NOT NULL,
	[fecha] [date] NOT NULL,
	[horario] [varchar](20) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[prestacion] [varchar](100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[zona] [varchar](60) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[estado] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Turno] PRIMARY KEY CLUSTERED
(
	[idTurno] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DatosClinicos](
	[idDatoClinico] [int] IDENTITY(1,1) NOT NULL,
	[idMascota] [int] NOT NULL,
	[peso] [decimal](6, 2) NOT NULL,
	[temperatura] [decimal](5, 2) NOT NULL,
	[frecuenciaCardiaca] [int] NOT NULL,
	[observaciones] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_DatosClinicos] PRIMARY KEY CLUSTERED
(
	[idDatoClinico] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Reintegro](
	[idReintegro] [int] IDENTITY(1,1) NOT NULL,
	[idCliente] [int] NOT NULL,
	[idMascota] [int] NOT NULL,
	[fechaSolicitud] [datetime] NOT NULL,
	[importe] [decimal](10, 2) NOT NULL,
	[estado] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[detalle] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Reintegro] PRIMARY KEY CLUSTERED
(
	[idReintegro] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[HistorialClinico](
	[idHistorial] [int] IDENTITY(1,1) NOT NULL,
	[idMascota] [int] NOT NULL,
	[fechaRegistro] [datetime] NOT NULL,
	[diagnostico] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[tratamiento] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[observaciones] [varchar](255) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_HistorialClinico] PRIMARY KEY CLUSTERED
(
	[idHistorial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Pago](
	[idPago] [int] IDENTITY(1,1) NOT NULL,
	[idSuscripcion] [int] NOT NULL,
	[importe] [decimal](10, 2) NOT NULL,
	[medioPago] [varchar](40) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[fechaPago] [date] NOT NULL,
	[estado] [varchar](30) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
	[dvh] [varchar](64) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
 CONSTRAINT [PK_Pago] PRIMARY KEY CLUSTERED
(
	[idPago] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]

GO
SET ANSI_PADDING ON

GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_Permiso_Pantalla] ON [dbo].[PermisoComponente]
(
	[pantalla] ASC
)
WHERE ([pantalla] IS NOT NULL)
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Cobertura_idPlan] ON [dbo].[Cobertura]
(
	[idPlan] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Usuario_Rol] ON [dbo].[Usuario]
(
	[idRol] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Cliente_idEmpresa] ON [dbo].[Cliente]
(
	[idEmpresa] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Bitacora_fecha_idBitacora] ON [dbo].[Bitacora]
(
	[fecha] DESC,
	[idBitacora] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Mascota_idCliente] ON [dbo].[Mascota]
(
	[idCliente] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Suscripcion_idCliente] ON [dbo].[Suscripcion]
(
	[idCliente] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Suscripcion_idPlan] ON [dbo].[Suscripcion]
(
	[idPlan] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Turno_idMascota] ON [dbo].[Turno]
(
	[idMascota] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
CREATE NONCLUSTERED INDEX [IX_Turno_Mascota] ON [dbo].[Turno]
(
	[idMascota] ASC,
	[fecha] DESC,
	[horario] DESC,
	[idTurno] DESC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
SET ANSI_PADDING ON

GO
CREATE UNIQUE NONCLUSTERED INDEX [UX_Turno_Confirmado] ON [dbo].[Turno]
(
	[zona] ASC,
	[prestacion] ASC,
	[fecha] ASC,
	[horario] ASC
)
WHERE ([estado]='Confirmado')
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_DatosClinicos_Mascota] ON [dbo].[DatosClinicos]
(
	[idMascota] ASC,
	[idDatoClinico] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Reintegro_idCliente] ON [dbo].[Reintegro]
(
	[idCliente] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Reintegro_idMascota] ON [dbo].[Reintegro]
(
	[idMascota] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_HistorialClinico_idMascota_fechaRegistro] ON [dbo].[HistorialClinico]
(
	[idMascota] ASC,
	[fechaRegistro] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_HistorialClinico_Mascota] ON [dbo].[HistorialClinico]
(
	[idMascota] ASC,
	[fechaRegistro] ASC,
	[idHistorial] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
CREATE NONCLUSTERED INDEX [IX_Pago_idSuscripcion] ON [dbo].[Pago]
(
	[idSuscripcion] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Plan] ADD  CONSTRAINT [DF_Plan_estado]  DEFAULT ((1)) FOR [estado]
GO
ALTER TABLE [dbo].[Plan] ADD  CONSTRAINT [DF_Plan_Corporativo]  DEFAULT ((0)) FOR [esCorporativo]
GO
ALTER TABLE [dbo].[__SchemaMigrations] ADD  CONSTRAINT [DF_SchemaMigrations_appliedUtc]  DEFAULT (sysutcdatetime()) FOR [appliedUtc]
GO
ALTER TABLE [dbo].[ErrorSistema] ADD  CONSTRAINT [DF_ErrorSistema_dvh]  DEFAULT ('') FOR [dvh]
GO
ALTER TABLE [dbo].[Empresa] ADD  CONSTRAINT [DF_Empresa_estado]  DEFAULT ((1)) FOR [estado]
GO
ALTER TABLE [dbo].[Cobertura] ADD  CONSTRAINT [DF_Cobertura_estado]  DEFAULT ((1)) FOR [estado]
GO
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_estado]  DEFAULT ((1)) FOR [estado]
GO
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_intentosFallidos]  DEFAULT ((0)) FOR [intentosFallidos]
GO
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_bloqueado]  DEFAULT ((0)) FOR [bloqueado]
GO
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_idioma]  DEFAULT ('es') FOR [idioma]
GO
ALTER TABLE [dbo].[Usuario] ADD  CONSTRAINT [DF_Usuario_Restablecer]  DEFAULT ((0)) FOR [requiereRestablecerContrasena]
GO
ALTER TABLE [dbo].[Cliente] ADD  CONSTRAINT [DF_Cliente_estado]  DEFAULT ((1)) FOR [estado]
GO
ALTER TABLE [dbo].[Bitacora] ADD  CONSTRAINT [DF_Bitacora_fecha]  DEFAULT (getdate()) FOR [fecha]
GO
ALTER TABLE [dbo].[Bitacora] ADD  CONSTRAINT [DF_Bitacora_Archivado]  DEFAULT ((0)) FOR [archivado]
GO
ALTER TABLE [dbo].[Mascota] ADD  CONSTRAINT [DF_Mascota_estado]  DEFAULT ((1)) FOR [estado]
GO
ALTER TABLE [dbo].[Cobertura]  WITH CHECK ADD  CONSTRAINT [FK_Cobertura_Plan] FOREIGN KEY([idPlan])
REFERENCES [dbo].[Plan] ([idPlan])
GO
ALTER TABLE [dbo].[Cobertura] CHECK CONSTRAINT [FK_Cobertura_Plan]
GO
ALTER TABLE [dbo].[Rol]  WITH CHECK ADD  CONSTRAINT [FK_Rol_Componente] FOREIGN KEY([idRol])
REFERENCES [dbo].[PermisoComponente] ([idComponente])
GO
ALTER TABLE [dbo].[Rol] CHECK CONSTRAINT [FK_Rol_Componente]
GO
ALTER TABLE [dbo].[RolPermiso]  WITH CHECK ADD  CONSTRAINT [FK_RolPermiso_Componente] FOREIGN KEY([idComponente])
REFERENCES [dbo].[PermisoComponente] ([idComponente])
GO
ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Componente]
GO
ALTER TABLE [dbo].[RolPermiso]  WITH CHECK ADD  CONSTRAINT [FK_RolPermiso_Rol] FOREIGN KEY([idRol])
REFERENCES [dbo].[Rol] ([idRol])
GO
ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [FK_RolPermiso_Rol]
GO
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD  CONSTRAINT [FK_Usuario_Rol] FOREIGN KEY([idRol])
REFERENCES [dbo].[Rol] ([idRol])
GO
ALTER TABLE [dbo].[Usuario] CHECK CONSTRAINT [FK_Usuario_Rol]
GO
ALTER TABLE [dbo].[Cliente]  WITH CHECK ADD  CONSTRAINT [FK_Cliente_Empresa] FOREIGN KEY([idEmpresa])
REFERENCES [dbo].[Empresa] ([idEmpresa])
GO
ALTER TABLE [dbo].[Cliente] CHECK CONSTRAINT [FK_Cliente_Empresa]
GO
ALTER TABLE [dbo].[Cliente]  WITH CHECK ADD  CONSTRAINT [FK_Cliente_Usuario] FOREIGN KEY([idUsuario])
REFERENCES [dbo].[Usuario] ([idUsuario])
GO
ALTER TABLE [dbo].[Cliente] CHECK CONSTRAINT [FK_Cliente_Usuario]
GO
ALTER TABLE [dbo].[Bitacora]  WITH CHECK ADD  CONSTRAINT [FK_Bitacora_Usuario] FOREIGN KEY([idUsuario])
REFERENCES [dbo].[Usuario] ([idUsuario])
GO
ALTER TABLE [dbo].[Bitacora] CHECK CONSTRAINT [FK_Bitacora_Usuario]
GO
ALTER TABLE [dbo].[Mascota]  WITH CHECK ADD  CONSTRAINT [FK_Mascota_Cliente] FOREIGN KEY([idCliente])
REFERENCES [dbo].[Cliente] ([idCliente])
GO
ALTER TABLE [dbo].[Mascota] CHECK CONSTRAINT [FK_Mascota_Cliente]
GO
ALTER TABLE [dbo].[Suscripcion]  WITH CHECK ADD  CONSTRAINT [FK_Suscripcion_Cliente] FOREIGN KEY([idCliente])
REFERENCES [dbo].[Cliente] ([idCliente])
GO
ALTER TABLE [dbo].[Suscripcion] CHECK CONSTRAINT [FK_Suscripcion_Cliente]
GO
ALTER TABLE [dbo].[Suscripcion]  WITH CHECK ADD  CONSTRAINT [FK_Suscripcion_Plan] FOREIGN KEY([idPlan])
REFERENCES [dbo].[Plan] ([idPlan])
GO
ALTER TABLE [dbo].[Suscripcion] CHECK CONSTRAINT [FK_Suscripcion_Plan]
GO
ALTER TABLE [dbo].[Turno]  WITH CHECK ADD  CONSTRAINT [FK_Turno_Mascota] FOREIGN KEY([idMascota])
REFERENCES [dbo].[Mascota] ([idMascota])
GO
ALTER TABLE [dbo].[Turno] CHECK CONSTRAINT [FK_Turno_Mascota]
GO
ALTER TABLE [dbo].[DatosClinicos]  WITH CHECK ADD  CONSTRAINT [FK_DatosClinicos_Mascota] FOREIGN KEY([idMascota])
REFERENCES [dbo].[Mascota] ([idMascota])
GO
ALTER TABLE [dbo].[DatosClinicos] CHECK CONSTRAINT [FK_DatosClinicos_Mascota]
GO
ALTER TABLE [dbo].[Reintegro]  WITH CHECK ADD  CONSTRAINT [FK_Reintegro_Cliente] FOREIGN KEY([idCliente])
REFERENCES [dbo].[Cliente] ([idCliente])
GO
ALTER TABLE [dbo].[Reintegro] CHECK CONSTRAINT [FK_Reintegro_Cliente]
GO
ALTER TABLE [dbo].[Reintegro]  WITH CHECK ADD  CONSTRAINT [FK_Reintegro_Mascota] FOREIGN KEY([idMascota])
REFERENCES [dbo].[Mascota] ([idMascota])
GO
ALTER TABLE [dbo].[Reintegro] CHECK CONSTRAINT [FK_Reintegro_Mascota]
GO
ALTER TABLE [dbo].[HistorialClinico]  WITH CHECK ADD  CONSTRAINT [FK_HistorialClinico_Mascota] FOREIGN KEY([idMascota])
REFERENCES [dbo].[Mascota] ([idMascota])
GO
ALTER TABLE [dbo].[HistorialClinico] CHECK CONSTRAINT [FK_HistorialClinico_Mascota]
GO
ALTER TABLE [dbo].[Pago]  WITH CHECK ADD  CONSTRAINT [FK_Pago_Suscripcion] FOREIGN KEY([idSuscripcion])
REFERENCES [dbo].[Suscripcion] ([idSuscripcion])
GO
ALTER TABLE [dbo].[Pago] CHECK CONSTRAINT [FK_Pago_Suscripcion]
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD FOREIGN KEY([idCliente])
REFERENCES [dbo].[Cliente] ([idCliente])
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD FOREIGN KEY([idPago])
REFERENCES [dbo].[Pago] ([idPago])
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD FOREIGN KEY([idPlan])
REFERENCES [dbo].[Plan] ([idPlan])
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD FOREIGN KEY([idSuscripcion])
REFERENCES [dbo].[Suscripcion] ([idSuscripcion])
GO
ALTER TABLE [dbo].[DetallePago]  WITH CHECK ADD FOREIGN KEY([idPago])
REFERENCES [dbo].[Pago] ([idPago])
GO
ALTER TABLE [dbo].[Plan]  WITH CHECK ADD  CONSTRAINT [CK_Plan_precio] CHECK  (([precio]>=(0)))
GO
ALTER TABLE [dbo].[Plan] CHECK CONSTRAINT [CK_Plan_precio]
GO
ALTER TABLE [dbo].[PermisoComponente]  WITH CHECK ADD CHECK  (([tipo]='Pantalla' OR [tipo]='Perfil'))
GO
ALTER TABLE [dbo].[AgendaTurno]  WITH CHECK ADD  CONSTRAINT [CK_AgendaTurno_Configuracion] CHECK  (([horaFin]>[horaInicio] AND ([duracionMinutos]>=(15) AND [duracionMinutos]<=(120)) AND ([horizonteDias]>=(1) AND [horizonteDias]<=(90))))
GO
ALTER TABLE [dbo].[AgendaTurno] CHECK CONSTRAINT [CK_AgendaTurno_Configuracion]
GO
ALTER TABLE [dbo].[__FormatoIntegridad]  WITH CHECK ADD  CONSTRAINT [CK_FormatoIntegridad_Id] CHECK  (([id]=(1)))
GO
ALTER TABLE [dbo].[__FormatoIntegridad] CHECK CONSTRAINT [CK_FormatoIntegridad_Id]
GO
ALTER TABLE [dbo].[ErrorSistema]  WITH CHECK ADD  CONSTRAINT [CK_ErrorSistema_criticidad] CHECK  (([criticidad]='Crítico' OR [criticidad]='Grave' OR [criticidad]='Advertencia'))
GO
ALTER TABLE [dbo].[ErrorSistema] CHECK CONSTRAINT [CK_ErrorSistema_criticidad]
GO
ALTER TABLE [dbo].[RolPermiso]  WITH CHECK ADD  CONSTRAINT [CK_RolPermiso_NoPropio] CHECK  (([idRol]<>[idComponente]))
GO
ALTER TABLE [dbo].[RolPermiso] CHECK CONSTRAINT [CK_RolPermiso_NoPropio]
GO
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD  CONSTRAINT [CK_Usuario_idioma] CHECK  (([idioma]='en' OR [idioma]='es'))
GO
ALTER TABLE [dbo].[Usuario] CHECK CONSTRAINT [CK_Usuario_idioma]
GO
ALTER TABLE [dbo].[Usuario]  WITH CHECK ADD  CONSTRAINT [CK_Usuario_intentosFallidos] CHECK  (([intentosFallidos]>=(0) AND [intentosFallidos]<=(3)))
GO
ALTER TABLE [dbo].[Usuario] CHECK CONSTRAINT [CK_Usuario_intentosFallidos]
GO
ALTER TABLE [dbo].[Bitacora]  WITH CHECK ADD  CONSTRAINT [CK_Bitacora_criticidad] CHECK  (([criticidad]='Crítica' OR [criticidad]='Grave' OR [criticidad]='Advertencia' OR [criticidad]='Informativa'))
GO
ALTER TABLE [dbo].[Bitacora] CHECK CONSTRAINT [CK_Bitacora_criticidad]
GO
ALTER TABLE [dbo].[Mascota]  WITH CHECK ADD  CONSTRAINT [CK_Mascota_fechaNacimiento] CHECK  (([fechaNacimiento]<=CONVERT([date],getdate())))
GO
ALTER TABLE [dbo].[Mascota] CHECK CONSTRAINT [CK_Mascota_fechaNacimiento]
GO
ALTER TABLE [dbo].[Suscripcion]  WITH CHECK ADD  CONSTRAINT [CK_Suscripcion_fechas] CHECK  (([fechaVencimiento]>=[fechaAlta]))
GO
ALTER TABLE [dbo].[Suscripcion] CHECK CONSTRAINT [CK_Suscripcion_fechas]
GO
ALTER TABLE [dbo].[DatosClinicos]  WITH CHECK ADD  CONSTRAINT [CK_DatosClinicos_Valores] CHECK  (([peso]>(0) AND [temperatura]>(0) AND [frecuenciaCardiaca]>(0)))
GO
ALTER TABLE [dbo].[DatosClinicos] CHECK CONSTRAINT [CK_DatosClinicos_Valores]
GO
ALTER TABLE [dbo].[Reintegro]  WITH CHECK ADD  CONSTRAINT [CK_Reintegro_importe] CHECK  (([importe]>=(0)))
GO
ALTER TABLE [dbo].[Reintegro] CHECK CONSTRAINT [CK_Reintegro_importe]
GO
ALTER TABLE [dbo].[Pago]  WITH CHECK ADD  CONSTRAINT [CK_Pago_importe] CHECK  (([importe]>=(0)))
GO
ALTER TABLE [dbo].[Pago] CHECK CONSTRAINT [CK_Pago_importe]
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD CHECK  (([estado]='Emitida'))
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD CHECK  (([importe]>(0)))
GO
ALTER TABLE [dbo].[Factura]  WITH CHECK ADD CHECK  (([moneda]='USD'))
GO
ALTER TABLE [dbo].[DetallePago]  WITH CHECK ADD CHECK  (([cantidadMascotas]>(0)))
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ReferenciaIntegridad_Obtener]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tabla, fila, columna, huella
    FROM dbo.ReferenciaIntegridad
    ORDER BY tabla, fila, columna;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ReferenciaIntegridad_Guardar]
    @tabla VARCHAR(60),
    @fila VARCHAR(30),
    @columna VARCHAR(60),
    @huella CHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.ReferenciaIntegridad
    SET huella = @huella
    WHERE tabla = @tabla AND fila = @fila AND columna = @columna;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.ReferenciaIntegridad (tabla, fila, columna, huella)
        VALUES (@tabla, @fila, @columna, @huella);
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ReferenciaIntegridad_EliminarFila]
    @tabla VARCHAR(60),
    @fila VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ReferenciaIntegridad
    WHERE tabla = @tabla AND fila = @fila;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_DVV_ObtenerPorTabla]
    @nombreTabla VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idDVV, nombreTabla, valorDVV
    FROM dbo.DVV
    WHERE nombreTabla = @nombreTabla;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_DVV_Guardar]
    @nombreTabla VARCHAR(60),
    @valorDVV VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.DVV
    SET valorDVV = @valorDVV
    WHERE nombreTabla = @nombreTabla;

    IF @@ROWCOUNT = 0
        INSERT INTO dbo.DVV (nombreTabla, valorDVV)
        VALUES (@nombreTabla, @valorDVV);
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ErrorSistema_ObtenerPorCodigo]
    @codigo VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT codigo, descripcion, criticidad
    FROM dbo.ErrorSistema
    WHERE codigo = @codigo;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_AsegurarCatalogo]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_InicializarCorporativo]
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE esCorporativo=1)
        UPDATE dbo.[Plan] SET esCorporativo=1,precio=COALESCE(precio,16)
        OUTPUT inserted.idPlan WHERE nombre='Plan Corporativo';
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_ObtenerContratable] @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE idPlan=@idPlan AND estado=1 AND precio>0
      AND esCorporativo=0;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_NecesitaCatalogo]
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK)) THEN 0 ELSE 1 END);
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_ListarContratables]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE estado=1 AND precio>0 AND esCorporativo=0
    ORDER BY precio,idPlan;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_AgendaTurno_Inicializar]
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    -- Configuración de demostración aprobada. Una agenda existente nunca se reemplaza.
    IF NOT EXISTS(SELECT 1 FROM dbo.AgendaTurno WITH(UPDLOCK,HOLDLOCK))
        INSERT dbo.AgendaTurno(zona,prestacion,horaInicio,horaFin,duracionMinutos,horizonteDias,estado,dvh)
        OUTPUT inserted.idAgendaTurno
        SELECT z.zona,p.prestacion,'09:00','18:00',30,30,1,''
        FROM (VALUES('CABA'),('Norte'),('Sur'),('Oeste')) z(zona)
        CROSS JOIN (VALUES('Consulta general'),('Vacunación'),('Control preventivo')) p(prestacion);
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Cobertura_ObtenerPorPlan] @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT prestacion,detalle FROM dbo.Cobertura WHERE idPlan=@idPlan AND estado=1 ORDER BY idCobertura;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Cobertura_AsegurarCatalogo]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ActualizarIdioma]
    @idUsuario INT,
    @idioma VARCHAR(2),
    @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT = 0
        THROW 50039, 'Se requiere una transaccion de integridad.', 1;
    IF @idioma IS NULL OR @idioma COLLATE Latin1_General_BIN2 NOT IN ('es', 'en')
        THROW 50025, 'Idioma no admitido.', 1;
    UPDATE dbo.Usuario
    SET idioma = @idioma, dvh = @dvh
    WHERE idUsuario = @idUsuario;
    IF @@ROWCOUNT <> 1
        THROW 50036, 'No existe el usuario indicado.', 1;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ActualizarEstadoSeguridad]
    @idUsuario INT,
    @intentosFallidos TINYINT,
    @bloqueado BIT,
    @estado BIT,
    @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuario
    SET intentosFallidos = @intentosFallidos,
        bloqueado = @bloqueado,
        estado = @estado,
        dvh = @dvh
    WHERE idUsuario = @idUsuario;

    IF @@ROWCOUNT = 0
        THROW 50002, 'No se encontró el usuario a actualizar.', 1;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ActualizarDVH]
    @idUsuario INT,
    @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuario
    SET dvh = @dvh
    WHERE idUsuario = @idUsuario;

    IF @@ROWCOUNT = 0
        THROW 50002, 'No se encontró el usuario indicado.', 1;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ActualizarContrasena] @idUsuario INT,@contrasena VARCHAR(255),@dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    UPDATE dbo.Usuario SET contrasena=@contrasena,requiereRestablecerContrasena=0,dvh=@dvh WHERE idUsuario=@idUsuario;
    IF @@ROWCOUNT<>1 THROW 50002,'No existe el usuario indicado.',1;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Bitacora_Registrar]
    @idUsuario INT = NULL,
    @fecha DATETIME,
    @accion VARCHAR(100),
    @modulo VARCHAR(50),
    @criticidad VARCHAR(20),
    @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Bitacora (idUsuario, fecha, accion, modulo, criticidad, dvh)
    VALUES (@idUsuario, @fecha, @accion, @modulo, @criticidad, @dvh);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS idBitacora;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE dbo.sp_Bitacora_ObtenerParaIntegridad
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh, archivado
    FROM dbo.Bitacora ORDER BY idBitacora;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE dbo.sp_Bitacora_ObtenerExcedentes
    @mantener INT
AS
BEGIN
    SET NOCOUNT ON;
    ;WITH RegistrosOrdenados AS
    (
        SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh, archivado,
            ROW_NUMBER() OVER (ORDER BY fecha DESC, idBitacora DESC) AS posicion
        FROM dbo.Bitacora
    )
    SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh, archivado
    FROM RegistrosOrdenados WHERE posicion > @mantener ORDER BY fecha, idBitacora;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Bitacora_ObtenerDvhs]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dvh
    FROM dbo.Bitacora
    ORDER BY idBitacora;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE dbo.sp_Bitacora_Eliminar
    @idBitacora INT,
    @dvh VARCHAR(64) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT = 0 OR @dvh IS NULL OR LEN(@dvh) <> 64
        THROW 50003, 'La baja lógica requiere transacción y verificador actualizado.', 1;
    UPDATE dbo.Bitacora SET archivado = 1, dvh = @dvh
    WHERE idBitacora = @idBitacora AND archivado = 0;
    IF @@ROWCOUNT = 0
        THROW 50003, 'No se encontró el registro activo de bitácora a archivar.', 1;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Bitacora_ActualizarDVH]
    @idBitacora INT,
    @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Bitacora
    SET dvh = @dvh
    WHERE idBitacora = @idBitacora;

    IF @@ROWCOUNT = 0
        THROW 50003, 'No se encontró el registro de bitácora indicado.', 1;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_ListarAdministracion] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'GestionPlanes')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    SELECT idPlan,nombre,descripcion,precio,estado,esCorporativo FROM dbo.[Plan] ORDER BY nombre,idPlan;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_ConsultarAdministracion] @idUsuario INT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'GestionPlanes,ConsultaPlan,ModificarPlan')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan) THROW 50011,'No existen datos asociados.',1;
    SELECT idPlan,nombre,descripcion,precio,estado,esCorporativo FROM dbo.[Plan] WHERE idPlan=@idPlan;
    SELECT idCobertura,prestacion,detalle,estado FROM dbo.Cobertura WHERE idPlan=@idPlan ORDER BY prestacion,idCobertura;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_Actualizar] @idUsuario INT,@nombre NVARCHAR(MAX),@descripcion NVARCHAR(MAX),@precio DECIMAL(28,10),@estado BIT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'ModificarPlan')=1 AND estado=1 AND bloqueado=0)
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Empresa_Registrar] @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX)
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- La BLL guarda DVH, DVV, referencias y auditoria dentro de la transaccion ambiente.
CREATE   PROCEDURE [dbo].[sp_Empresa_Listar] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'Empresas,RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    SELECT idEmpresa,razonSocial,cuit,direccion,telefono,estado FROM dbo.vw_EmpresaDescifrado ORDER BY razonSocial,idEmpresa;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Empresa_CuitRegistrado] @idUsuario INT,@cuit VARCHAR(20),@idEmpresa INT=0
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaEmpresa,ModificarEmpresa')=1 AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    IF @cuit IS NULL OR LEN(@cuit)<>11 OR @cuit COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' THROW 50025, 'Datos invalidos.', 1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.vw_EmpresaDescifrado WITH(UPDLOCK,HOLDLOCK) WHERE REPLACE(cuit,'-','')=@cuit AND idEmpresa<>@idEmpresa) THEN 1 ELSE 0 END);
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Empresa_Consultar] @idUsuario INT,@idEmpresa INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'Empresas,ConsultaEmpresa,ModificarEmpresa,RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_EmpresaDescifrado WHERE idEmpresa=@idEmpresa) THROW 50011, 'No existen datos asociados.', 1;
    SELECT idEmpresa,razonSocial,cuit,direccion,telefono,estado FROM dbo.vw_EmpresaDescifrado WHERE idEmpresa=@idEmpresa;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Empresa_Actualizar] @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX),@idEmpresa INT
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Cliente_Registrar]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ClienteCorporativo_CorreoRegistrado] @idUsuario INT,@correo VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50074,'Se requiere una transaccion de integridad.',1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WITH(UPDLOCK,HOLDLOCK) WHERE correoElectronico=@correo) THEN 1 ELSE 0 END);
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ClienteCorporativo_ConsultarPlan] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50074,'Se requiere una transaccion de integridad.',1;
    SELECT TOP(1) idPlan,nombre,precio FROM dbo.[Plan] WHERE esCorporativo=1 AND estado=1 ORDER BY idPlan;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ClienteCorporativo_RegistrarUsuario]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Cobertura_GuardarAdministracion] @idUsuario INT,@idPlan INT,@idCobertura INT,@prestacion NVARCHAR(MAX),@detalle NVARCHAR(MAX),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaPlan,ModificarPlan')=1 AND estado=1 AND bloqueado=0)
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_RegistrarCliente]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ObtenerPorId] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*,p.nombre AS rol FROM dbo.vw_UsuarioDescifrado u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol WHERE idUsuario=@idUsuario;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ObtenerPorCorreo] @correoElectronico VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*,p.nombre AS rol FROM dbo.vw_UsuarioDescifrado u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol WHERE correoElectronico=@correoElectronico;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ObtenerParaIntegridad]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*,p.nombre AS rol FROM dbo.vw_UsuarioDescifrado u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol ORDER BY idUsuario;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_ObtenerDvhs]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dvh
    FROM dbo.vw_UsuarioDescifrado
    ORDER BY idUsuario;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_Listar]
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
        (SELECT 1 FROM dbo.vw_UsuarioDescifrado) THEN 1 ELSE 0 END);

    SELECT u.idUsuario,u.nombre,u.correoElectronico,u.idRol,p.nombre AS rol,u.estado,u.bloqueado
    FROM dbo.vw_UsuarioDescifrado u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol
    WHERE (@nombre = N'' OR CHARINDEX(@nombre,
           u.nombre COLLATE Latin1_General_CI_AI) > 0)
      AND (@estado = 0
           OR (@estado = 1 AND estado = 1)
           OR (@estado = 2 AND estado = 0)
           OR (@estado = 3 AND bloqueado = 1)
           OR (@estado = 4 AND bloqueado = 0))
    ORDER BY u.nombre, idUsuario;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Usuario_GuardarAdministracion]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Plan_Registrar] @idUsuario INT,@nombre NVARCHAR(MAX),@descripcion NVARCHAR(MAX),@precio DECIMAL(28,10),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaPlan')=1 AND estado=1 AND bloqueado=0)
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Suscripcion_PrepararContratacion]
    @idUsuario INT, @idPlan INT, @versionPlan VARCHAR(64), @cantidadEsperada INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.vw_ClienteDescifrado c WITH(UPDLOCK,HOLDLOCK)
        INNER JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
        WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'ContratarPlan')=1 AND u.estado=1 AND u.bloqueado=0);
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- CU-001-004. Reutiliza Cliente, Suscripcion, Plan, Cobertura y la auditoria existente.
CREATE   PROCEDURE [dbo].[sp_Suscripcion_ObtenerVigenteCliente] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50058, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.vw_ClienteDescifrado WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    DECLARE @hoy DATE=CONVERT(DATE,GETDATE());
    IF NOT EXISTS(SELECT 1 FROM dbo.Suscripcion WHERE idCliente=@cliente AND estado='Activa'
      AND fechaAlta<=@hoy AND fechaVencimiento>=@hoy)
      THROW 50008, 'No existe una cobertura asociada.', 1;
    SELECT TOP(1) s.idSuscripcion,s.idPlan,p.nombre,p.descripcion,s.estado,s.fechaAlta,s.fechaVencimiento
      FROM dbo.Suscripcion s INNER JOIN dbo.[Plan] p ON p.idPlan=s.idPlan
      WHERE s.idCliente=@cliente AND s.estado='Activa' AND s.fechaAlta<=@hoy AND s.fechaVencimiento>=@hoy
      ORDER BY s.fechaAlta DESC,s.idSuscripcion DESC;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Suscripcion_ObtenerPendiente] @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.idSuscripcion,s.idPlan FROM dbo.Suscripcion s
    INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=s.idCliente
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND s.idSuscripcion=@idSuscripcion AND s.estado='Pendiente';
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Cobertura_ConsultarCuenta] @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50058, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.vw_ClienteDescifrado WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    DECLARE @plan INT,@hoy DATE=CONVERT(DATE,GETDATE());
    SELECT @plan=idPlan FROM dbo.Suscripcion WHERE idSuscripcion=@idSuscripcion AND idCliente=@cliente
      AND estado='Activa' AND fechaAlta<=@hoy AND fechaVencimiento>=@hoy;
    IF @plan IS NULL THROW 50008, 'No existe una cobertura asociada.', 1;
    SELECT prestacion,detalle FROM dbo.Cobertura WHERE idPlan=@plan AND estado=1 ORDER BY idCobertura;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ClienteCorporativo_RegistrarCliente]
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_ClienteCorporativo_AsignarPlan] @idAdministrador INT,@idCliente INT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_UsuarioDescifrado WHERE idUsuario=@idAdministrador AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50007,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_ClienteDescifrado c JOIN dbo.vw_EmpresaDescifrado e ON e.idEmpresa=c.idEmpresa WHERE c.idCliente=@idCliente AND c.tipoCliente='Corporativo' AND c.estado=1 AND e.estado=1)
        THROW 50072,'Empresa o cliente no disponible.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan AND esCorporativo=1 AND estado=1) THROW 50073,'No se encuentra disponible el plan Corporativo.',1;
    IF EXISTS(SELECT 1 FROM dbo.Suscripcion WHERE idCliente=@idCliente) THROW 50007,'El cliente ya posee una suscripcion.',1;
    DECLARE @hoy DATE=CONVERT(DATE,GETDATE());
    INSERT dbo.Suscripcion(idCliente,idPlan,fechaAlta,fechaVencimiento,estado,dvh)
    OUTPUT inserted.idSuscripcion VALUES(@idCliente,@idPlan,@hoy,DATEADD(MONTH,1,@hoy),'Activa','');
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Cliente_ObtenerActivoUsuario] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50060, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.vw_ClienteDescifrado c INNER JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Cliente,Planes,ContratarPlan,GestionPago,RegistroMascota,Mascotas,ActualizarMascota,ConsultaMascota,HistorialClinico,DatosClinicos,Turnos,ConsultaCobertura,Reintegros')=1 AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT @cliente idCliente;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Cliente_CotizarPlan] @idUsuario INT, @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idCliente,p.idPlan,p.nombre,p.descripcion,p.precio,p.dvh,
        (SELECT COUNT(*) FROM dbo.Mascota m WHERE m.idCliente=c.idCliente AND m.estado=1) cantidadMascotas
    FROM dbo.vw_ClienteDescifrado c INNER JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
    CROSS JOIN dbo.[Plan] p
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Planes,ContratarPlan')=1 AND u.estado=1 AND u.bloqueado=0
      AND p.idPlan=@idPlan AND p.estado=1 AND p.precio>0
      AND p.esCorporativo=0;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Mascota_Registrar]
    @idUsuario INT,@nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50060, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.vw_ClienteDescifrado c INNER JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'RegistroMascota')=1 AND u.estado=1 AND u.bloqueado=0);
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Mascota_ListarUsuario] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50061, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.vw_ClienteDescifrado c JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Mascotas,Turnos')=1 AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT idMascota,idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh
    FROM dbo.Mascota WHERE idCliente=@cliente ORDER BY nombre,idMascota;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Mascota_ConsultarUsuario] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50061, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=m.idCliente
      JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Mascotas,ActualizarMascota,ConsultaMascota,HistorialClinico,DatosClinicos,Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idMascota,idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh
    FROM dbo.Mascota WHERE idMascota=@idMascota;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE dbo.sp_Mascota_Actualizar
 @idUsuario INT,@idMascota INT,@dvhOriginal VARCHAR(64),
 @nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50062, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @dvhActual VARCHAR(64);
    SELECT @dvhActual=m.dvh
    FROM dbo.Mascota m JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=m.idCliente
      JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'ActualizarMascota')=1 AND u.estado=1 AND u.bloqueado=0;
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
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_AgendaTurno_ListarZonas] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.vw_ClienteDescifrado c JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
        WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    SELECT DISTINCT zona FROM dbo.AgendaTurno WHERE estado=1 ORDER BY zona;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_HistorialClinico_ConsultarMascota] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50063, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=m.idCliente
      JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'HistorialClinico')=1 AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idHistorial,fechaRegistro,diagnostico,tratamiento,observaciones
    FROM dbo.HistorialClinico WHERE idMascota=@idMascota ORDER BY fechaRegistro DESC,idHistorial DESC;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Pago_ObtenerComprobante] @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.idPago,p.idSuscripcion,s.idPlan,pl.nombre,p.importe,p.medioPago,p.fechaPago,p.estado
      FROM dbo.Pago p INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion
      INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=s.idCliente INNER JOIN dbo.[Plan] pl ON pl.idPlan=s.idPlan
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND p.idSuscripcion=@idSuscripcion AND p.estado='Aprobado'
      ORDER BY p.idPago;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Pago_ConsultarPendiente] @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    DECLARE @cliente INT,@plan INT;
    SELECT @cliente=c.idCliente,@plan=s.idPlan FROM dbo.Suscripcion s WITH(UPDLOCK,HOLDLOCK)
    INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=s.idCliente
    WHERE s.idSuscripcion=@idSuscripcion AND c.idUsuario=@idUsuario AND c.estado=1 AND s.estado='Pendiente';
    IF @cliente IS NULL OR EXISTS(SELECT 1 FROM dbo.Pago WITH(UPDLOCK,HOLDLOCK) WHERE idSuscripcion=@idSuscripcion AND estado='Aprobado') THROW 50054,'La suscripcion no admite un nuevo pago.',1;
    DECLARE @precio DECIMAL(10,2),@nombre VARCHAR(80);
    SELECT @precio=precio,@nombre=nombre FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK)
    WHERE idPlan=@plan AND estado=1 AND precio>0 AND nombre IN ('Plan Esencial','Plan Preventivo','Plan Integral');
    IF @precio IS NULL THROW 50050,'Plan no disponible.',1;
    DECLARE @cantidad INT=(SELECT COUNT(*) FROM dbo.Mascota WITH(UPDLOCK,HOLDLOCK) WHERE idCliente=@cliente AND estado=1);
    IF @cantidad=0 THROW 50052,'No existen mascotas activas.',1;
    DECLARE @importe DECIMAL(38,2)=CONVERT(DECIMAL(20,2),@precio)+CONVERT(DECIMAL(20,2),ROUND(@precio*0.9,2))*(@cantidad-1);
    IF @importe<=0 OR @importe>99999999.99 THROW 50025,'Importe invalido.',1;
    SELECT @idSuscripcion idSuscripcion,@plan idPlan,@nombre nombre,@precio precio,@cantidad cantidadMascotas,CONVERT(DECIMAL(10,2),@importe) importe;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_DatosClinicos_ConsultarMascota] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50063, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=m.idCliente
      JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'DatosClinicos')=1 AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idDatoClinico,peso,temperatura,frecuenciaCardiaca,observaciones
    FROM dbo.DatosClinicos WHERE idMascota=@idMascota ORDER BY idDatoClinico DESC;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Suscripcion_ActivarPagada] @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT,@estado VARCHAR(30);
    SELECT @cliente=c.idCliente,@estado=s.estado FROM dbo.Suscripcion s WITH(UPDLOCK,HOLDLOCK)
      INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=s.idCliente
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

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- CU-001-005. Consulta de solo lectura; reutiliza Cliente y Reintegro.
CREATE   PROCEDURE [dbo].[sp_Reintegro_ListarCliente] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50059, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.vw_ClienteDescifrado WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT idReintegro,fechaSolicitud,importe,estado,detalle FROM dbo.Reintegro
      WHERE idCliente=@cliente ORDER BY fechaSolicitud DESC,idReintegro DESC;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Reintegro_ConsultarCliente] @idUsuario INT,@idReintegro INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50059, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.vw_ClienteDescifrado WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Reintegro WHERE idReintegro=@idReintegro AND idCliente=@cliente)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idReintegro,fechaSolicitud,importe,estado,detalle FROM dbo.Reintegro
      WHERE idReintegro=@idReintegro AND idCliente=@cliente;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Turno_ConsultarMascota] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=m.idCliente JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
        WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    SELECT idTurno,idMascota,fecha,horario,prestacion,zona,estado FROM dbo.Turno WHERE idMascota=@idMascota
    ORDER BY fecha DESC,horario DESC,idTurno DESC;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Turno_ConsultarDisponibilidad] @idUsuario INT,@idMascota INT,@zona VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=m.idCliente JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
        WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    IF NULLIF(LTRIM(RTRIM(@zona)),'') IS NULL THROW 50002, 'Datos incompletos.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.AgendaTurno WHERE zona=@zona AND estado=1) THROW 50025, 'Datos invalidos.', 1;
    DECLARE @ahora DATETIME=GETDATE(), @hoy DATE=CONVERT(DATE,GETDATE());
    ;WITH digito(n) AS (SELECT n FROM(VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9))x(n)),
    numero(n) AS (SELECT a.n+10*b.n FROM digito a CROSS JOIN digito b)
    SELECT a.idAgendaTurno,a.prestacion,f.fecha,CONVERT(VARCHAR(5),h.hora,108) AS horario,a.zona
    FROM dbo.AgendaTurno a CROSS JOIN numero dias CROSS JOIN numero bloques
    CROSS APPLY(SELECT DATEADD(DAY,dias.n,@hoy) AS fecha) f
    CROSS APPLY(SELECT DATEADD(MINUTE,bloques.n*a.duracionMinutos,a.horaInicio) AS hora) h
    WHERE a.estado=1 AND a.zona=@zona AND dias.n<a.horizonteDias
      AND DATEDIFF(DAY,CONVERT(DATE,'19000101'),f.fecha)%7 BETWEEN 0 AND 5
      AND bloques.n*a.duracionMinutos+a.duracionMinutos<=DATEDIFF(MINUTE,a.horaInicio,a.horaFin)
      AND DATEADD(MINUTE,DATEDIFF(MINUTE,CONVERT(TIME,'00:00'),h.hora),CONVERT(DATETIME,f.fecha))>@ahora
      AND NOT EXISTS(SELECT 1 FROM dbo.Turno t WHERE t.zona=a.zona AND t.prestacion=a.prestacion
          AND t.fecha=f.fecha AND t.horario=CONVERT(VARCHAR(5),h.hora,108) AND t.estado='Confirmado')
    ORDER BY f.fecha,h.hora,a.prestacion;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Turno_VerificarDisponibilidad]
    @idUsuario INT,@idMascota INT,@idAgendaTurno INT,@fecha DATE,@horario VARCHAR(20),@devolver BIT=1
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=m.idCliente JOIN dbo.vw_UsuarioDescifrado u ON u.idUsuario=c.idUsuario
        WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    IF @idAgendaTurno IS NULL OR @fecha IS NULL OR NULLIF(@horario,'') IS NULL THROW 50002, 'Datos incompletos.', 1;
    DECLARE @hora TIME(0)=TRY_CONVERT(TIME(0),@horario),@ahora DATETIME=GETDATE(),@hoy DATE=CONVERT(DATE,GETDATE());
    IF @hora IS NULL OR @horario<>CONVERT(VARCHAR(5),@hora,108) THROW 50025, 'Datos invalidos.', 1;
    DECLARE @zona VARCHAR(60),@prestacion VARCHAR(100),@inicio TIME(0),@fin TIME(0),@duracion INT,@horizonte INT;
    SELECT @zona=zona,@prestacion=prestacion,@inicio=horaInicio,@fin=horaFin,@duracion=duracionMinutos,@horizonte=horizonteDias
    FROM dbo.AgendaTurno WHERE idAgendaTurno=@idAgendaTurno AND estado=1;
    IF @zona IS NULL THROW 50013, 'El turno ya no se encuentra disponible.', 1;
    IF @fecha<@hoy OR DATEADD(MINUTE,DATEDIFF(MINUTE,CONVERT(TIME,'00:00'),@hora),CONVERT(DATETIME,@fecha))<=@ahora
        THROW 50013, 'El turno ya no se encuentra disponible.', 1;
    IF @fecha>=DATEADD(DAY,@horizonte,@hoy) OR DATEDIFF(DAY,CONVERT(DATE,'19000101'),@fecha)%7 NOT BETWEEN 0 AND 5
      OR @hora<@inicio OR DATEDIFF(MINUTE,@inicio,@hora)%@duracion<>0
      OR DATEDIFF(MINUTE,@inicio,@hora)+@duracion>DATEDIFF(MINUTE,@inicio,@fin)
        THROW 50025, 'Datos invalidos.', 1;
    IF EXISTS(SELECT 1 FROM dbo.Turno WITH(UPDLOCK,HOLDLOCK) WHERE zona=@zona AND prestacion=@prestacion
        AND fecha=@fecha AND horario=@horario AND estado='Confirmado')
        THROW 50013, 'El turno ya no se encuentra disponible.', 1;
    IF @devolver=1 SELECT @idAgendaTurno AS idAgendaTurno,@prestacion AS prestacion,@fecha AS fecha,@horario AS horario,@zona AS zona;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Turno_Registrar]
    @idUsuario INT,@idMascota INT,@idAgendaTurno INT,@fecha DATE,@horario VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50066, 'Se requiere una transaccion de integridad.', 1;
    EXEC dbo.sp_Turno_VerificarDisponibilidad @idUsuario,@idMascota,@idAgendaTurno,@fecha,@horario,0;
    BEGIN TRY
        INSERT dbo.Turno(idMascota,fecha,horario,prestacion,zona,estado,dvh)
        OUTPUT inserted.idTurno,inserted.idMascota,inserted.fecha,inserted.horario,inserted.prestacion,inserted.zona,inserted.estado
        SELECT @idMascota,@fecha,@horario,prestacion,zona,'Confirmado','' FROM dbo.AgendaTurno WHERE idAgendaTurno=@idAgendaTurno;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50013, 'El turno ya no se encuentra disponible.', 1;
        THROW;
    END CATCH;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE dbo.sp_Pago_Registrar
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
    SELECT @pago,precio,ROUND(precio*0.9,2),cantidadMascotas,dbo.fn_CifrarDato(@titular,N'DetallePago.titular'),dbo.fn_CifrarDato(@correoFactura,N'DetallePago.correoFactura'),'' FROM @resumen;
    SELECT @pago;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE dbo.sp_Factura_Obtener @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.*,d.correoFactura FROM dbo.vw_FacturaDescifrado f INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=f.idCliente
    LEFT JOIN dbo.vw_DetallePagoDescifrado d ON d.idPago=f.idPago
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND f.idSuscripcion=@idSuscripcion;
END;
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE [dbo].[sp_Factura_ListarPropias] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.idSuscripcion,pl.nombre planNombre,p.importe,p.fechaPago,f.numeroFactura
    FROM dbo.Pago p INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion
    INNER JOIN dbo.vw_ClienteDescifrado c ON c.idCliente=s.idCliente INNER JOIN dbo.[Plan] pl ON pl.idPlan=s.idPlan LEFT JOIN dbo.vw_FacturaDescifrado f ON f.idPago=p.idPago
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND p.estado='Aprobado' ORDER BY p.fechaPago DESC,p.idPago DESC;
END;

GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE   PROCEDURE dbo.sp_Factura_Emitir @idUsuario INT,@idSuscripcion INT
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
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'001_InitialSchema.sql','76eeb0be03e9b21f13563dfc961d351a25b24172bbb3be0f85f39fed930a46a4');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'002_SeedData.sql','1d45458cbd6a545f0fc2e57561bc2470e2e545409a296d786293081092d9268a');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'003_StoredProcedures.sql','a259245ecbe3ae9ae7a1c411c7a07846208c87434952235d57160df2ff1785ac');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'004_ReferenciasIntegridad.sql','15f9378588186360eb5f9cd855c87874f614bcfb06108b806711b4230de2fab0');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'005_ErroresValidacionLogin.sql','6e0a50f667c5c86dbaa402a2b77cae71cb84ec698296bdfc6e1666694a8b6b54');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'006_HuellasSinClave.sql','bc772b3f4575b7026428b1839bd945a92ea941bf1e631f5b869bbd6822c28a2f');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'007_ConsultaBitacora.sql','52c65c5e61426bce44ff1a5d10cd262c9991759cb0447fb7fe8616a63a887bdd');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'008_ErrorAuditoriaBitacora.sql','c6f701c05fd35c9768cd2c000cb676c094df1a3844cb113e643c6fdc81169431');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'009_ErrorLogout.sql','ca4fb9cfffffd034e3036d4248a658c8c7a7acbf290dff0325c21395d8d71c63');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'010_GestionUsuarios.sql','41efa4a1f41cd9fd7a4919c2883b091577a19ee0ee38b891f10efc1cd308180a');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'011_OperacionesUsuarios.sql','6aaf782cd89a3f1653124fb31131a13adc1bcc3784b466b5e8052ff8fc7d94b9');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'012_IntegridadTodasLasTablas.sql','3d63380e664068ed267ff9cbc897c64d1ecbe2f62f08c65df637f969d93313b1');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'013_ProcedimientosSimples.sql','699eaca3914c31a0683c5c24a67df39b3e18f1056f26b2f5b943935ae533e6e2');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'014_IdiomaUsuario.sql','a6ce01e14a541557929fc71843c65ad5c8e3973e639f6b740ab27bc1643e06de');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'015_RegistroCliente.sql','013c61a137c14d3e0ac6deec2c8e3c372909176da81cd8ba19502c324f46f584');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'016_ContratacionPlanes.sql','468fe692c3917c2cb4ac1b3033540605fe1290415d0ea803bbce81a4a58770d3');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'017_GestionPago.sql','d580ebe591d1cde9f5c4b13778cf264e8d3136faead194fb3a19a2284efd65c7');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'018_ConsultaCobertura.sql','2fd6d5270aafb04956e5105fc9d5300b9773e5c9271f627a25322a938e618d77');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'019_ConsultaReintegros.sql','8c96b6b987929116fc557d3e35e02c800073c05694df161949bc5a6e57bba90b');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'020_RegistroMascota.sql','85b9c4ba8cda8c1074cb6eee82e10bab4bfc6fdc5029e5591cfc0d5f863144f6');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'021_GestionMascotas.sql','7654f2dd20bea7ae9bbc1b5d7437cd410895c00a66fbcfcfd1939492f58d62bc');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'022_Turnos.sql','643507b7c09db1cd3a01128791b51be1009c2ee6f9a4bd938fff104dc23acceb');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'023_Empresas.sql','00ab1896acee59c6d7f154d9c186714c0d2199cf6266a17848e0f96b03f6021e');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'024_ClienteCorporativo.sql','805038c0f2dd3a3e502aca620ac995cedf2a47aab8abf48d81a72a730a8e35b2');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'025_AdministracionPlanes.sql','174ffaab3506c14703e39854f2cc209ed4b999f910106e326894407520485b0c');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'026_SimplificacionProcedimientos.sql','0dc14c21ebd74add41075f94cdb32f8ee8f159cf2c0ebbfc8d11de683c33890a');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'027_Facturacion.sql','6d11a215ee8a7f75b52b281694c1d6e562455d652e1d727fe295643a4e3f91d6');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'028_OptimizarLecturas.sql','8bfe8580c14ae22bcb008913ea593c42c34b9c24cf62adbf000e2ddd992c4c22');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'029_CorreoFactura.sql','b921089fbc2848a1e156d5e74fcbafafbeb8cbf39993316ad654ed3c9f78a157');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'030_PerfilesComposite.sql','2159932cc2dfdadbe944f26aa5a8e4d4edb115dbc4692f2933867e04fafe8418');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'031_AutorizacionPorPantallas.sql','3a3528113730f2b5c87724b78e483b46e2308c5e6125997d7d4f81ef2da95686');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'032_RolesRelacionales.sql','746f6f0a7d3fe72b80ed9691ff9d7e9729e1ce24963215c38d01c76dd93616d2');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'033_EliminarPermisoRelacion.sql','8b273e270d81cd994854d79e3dd8190ce94a36700da690ac2bf612c2c34ad23e');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'034_CifradoDatosPersonales.sql','cdbb249765714e403a04f06bfbad7e8a74d15829e765f12feaee691dc26d3b09');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'035_BitacoraBajaLogica.sql','a770337d1d645c89c6834fed63148a3f6ebcfb3bcc40f49e8a5204992c49e24e');
INSERT dbo.__SchemaMigrations(migrationId,checksum) VALUES(N'036_DescuentoMascotaAdicional.sql','7a4235d61e943d857a11f43ef0c3efc81196c2a8824a33039466f78ad3fe315d');
