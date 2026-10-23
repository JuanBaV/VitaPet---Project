USE [VitaPet]
GO

CREATE OR ALTER FUNCTION dbo.fn_RolPermite(@idRol INT,@pantallas VARCHAR(1000))
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

CREATE OR ALTER PROCEDURE [dbo].[sp_AgendaTurno_Inicializar]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_AgendaTurno_ListarZonas] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Cliente c JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
        WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    SELECT DISTINCT zona FROM dbo.AgendaTurno WHERE estado=1 ORDER BY zona;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Bitacora_ActualizarDVH]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Bitacora_Eliminar]
    @idBitacora INT
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.Bitacora
    WHERE idBitacora = @idBitacora;

    IF @@ROWCOUNT = 0
        THROW 50003, 'No se encontró el registro de bitácora a eliminar.', 1;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Bitacora_ObtenerDvhs]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dvh
    FROM dbo.Bitacora
    ORDER BY idBitacora;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Bitacora_ObtenerExcedentes]
    @mantener INT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH RegistrosOrdenados AS
    (
        SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh,
               ROW_NUMBER() OVER (ORDER BY fecha DESC, idBitacora DESC) AS posicion
        FROM dbo.Bitacora
    )
    SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh
    FROM RegistrosOrdenados
    WHERE posicion > @mantener
    ORDER BY fecha ASC, idBitacora ASC;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Bitacora_ObtenerParaIntegridad]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh
    FROM dbo.Bitacora
    ORDER BY idBitacora;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Bitacora_Registrar]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Cliente_CotizarPlan] @idUsuario INT, @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT c.idCliente,p.idPlan,p.nombre,p.descripcion,p.precio,p.dvh,
        (SELECT COUNT(*) FROM dbo.Mascota m WHERE m.idCliente=c.idCliente AND m.estado=1) cantidadMascotas
    FROM dbo.Cliente c INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
    CROSS JOIN dbo.[Plan] p
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Planes,ContratarPlan')=1 AND u.estado=1 AND u.bloqueado=0
      AND p.idPlan=@idPlan AND p.estado=1 AND p.precio>0
      AND p.esCorporativo=0;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Cliente_ObtenerActivoUsuario] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50060, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Cliente,Planes,ContratarPlan,GestionPago,RegistroMascota,Mascotas,ActualizarMascota,ConsultaMascota,HistorialClinico,DatosClinicos,Turnos,ConsultaCobertura,Reintegros')=1 AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT @cliente idCliente;
END;
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
    VALUES(@idUsuario, NULL, @nombre, @apellido, @telefono, 'Individual', 1, '');
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ClienteCorporativo_AsignarPlan] @idAdministrador INT,@idCliente INT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idAdministrador AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
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

CREATE OR ALTER PROCEDURE [dbo].[sp_ClienteCorporativo_ConsultarPlan] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50074,'Se requiere una transaccion de integridad.',1;
    SELECT TOP(1) idPlan,nombre,precio FROM dbo.[Plan] WHERE esCorporativo=1 AND estado=1 ORDER BY idPlan;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ClienteCorporativo_CorreoRegistrado] @idUsuario INT,@correo VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50074,'Se requiere una transaccion de integridad.',1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.Usuario WITH(UPDLOCK,HOLDLOCK) WHERE correoElectronico=@correo) THEN 1 ELSE 0 END);
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ClienteCorporativo_RegistrarCliente]
    @idAdministrador INT,@idUsuario INT,@idEmpresa INT,@nombre NVARCHAR(MAX),@apellido NVARCHAR(MAX),@telefono NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idAdministrador AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empresa WHERE idEmpresa=@idEmpresa) THROW 50011,'No existen datos asociados.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empresa WHERE idEmpresa=@idEmpresa AND estado=1) THROW 50072,'La empresa seleccionada se encuentra inactiva.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND idRol=(SELECT idComponente FROM dbo.PermisoComponente WHERE nombre='Cliente' AND tipo='Perfil' AND sistema=1) AND estado=1 AND requiereRestablecerContrasena=1)
       OR EXISTS(SELECT 1 FROM dbo.Cliente WHERE idUsuario=@idUsuario) THROW 50049,'Cuenta no disponible para el alta corporativa.',1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@apellido)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@telefono)),N'') IS NULL THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>60 OR LEN(@apellido)>60 OR LEN(@nombre+N' '+@apellido)>100 OR LEN(@telefono)>30 THROW 50025,'Datos invalidos.',1;
    INSERT dbo.Cliente(idUsuario,idEmpresa,nombre,apellido,telefono,tipoCliente,estado,dvh)
    OUTPUT inserted.idCliente VALUES(@idUsuario,@idEmpresa,@nombre,@apellido,@telefono,'Corporativo',1,'');
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ClienteCorporativo_RegistrarUsuario]
    @idAdministrador INT,@nombre NVARCHAR(MAX),@correo NVARCHAR(MAX),@hash VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idAdministrador AND dbo.fn_RolPermite(idRol,'RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),N'') IS NULL OR NULLIF(LTRIM(RTRIM(@correo)),N'') IS NULL THROW 50002,'Datos incompletos.',1;
    IF LEN(@nombre)>100 OR LEN(@correo)>100 OR LEN(@hash)<>64 OR @hash IS NULL OR @hash COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-f]%'
       OR @nombre COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@nombre)) COLLATE Latin1_General_100_BIN2
       OR @correo COLLATE Latin1_General_100_BIN2<>CONVERT(NVARCHAR(MAX),CONVERT(VARCHAR(MAX),@correo)) COLLATE Latin1_General_100_BIN2
        THROW 50025,'Datos invalidos.',1;
    IF EXISTS(SELECT 1 FROM dbo.Usuario WITH(UPDLOCK,HOLDLOCK) WHERE correoElectronico=@correo) THROW 50001,'Correo ya registrado.',1;
    BEGIN TRY
        INSERT dbo.Usuario(nombre,correoElectronico,contrasena,idRol,estado,intentosFallidos,bloqueado,idioma,requiereRestablecerContrasena,dvh)
        OUTPUT inserted.idUsuario VALUES(@nombre,@correo,@hash,(SELECT idRol FROM dbo.Rol r JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol WHERE p.nombre='Cliente' AND p.sistema=1),1,0,0,'es',1,'');
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50001,'Correo ya registrado.',1;
        THROW;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Cobertura_AsegurarCatalogo]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Cobertura_ConsultarCuenta] @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50058, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    DECLARE @plan INT,@hoy DATE=CONVERT(DATE,GETDATE());
    SELECT @plan=idPlan FROM dbo.Suscripcion WHERE idSuscripcion=@idSuscripcion AND idCliente=@cliente
      AND estado='Activa' AND fechaAlta<=@hoy AND fechaVencimiento>=@hoy;
    IF @plan IS NULL THROW 50008, 'No existe una cobertura asociada.', 1;
    SELECT prestacion,detalle FROM dbo.Cobertura WHERE idPlan=@plan AND estado=1 ORDER BY idCobertura;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Cobertura_GuardarAdministracion] @idUsuario INT,@idPlan INT,@idCobertura INT,@prestacion NVARCHAR(MAX),@detalle NVARCHAR(MAX),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaPlan,ModificarPlan')=1 AND estado=1 AND bloqueado=0)
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Cobertura_ObtenerPorPlan] @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT prestacion,detalle FROM dbo.Cobertura WHERE idPlan=@idPlan AND estado=1 ORDER BY idCobertura;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_DatosClinicos_ConsultarMascota] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50063, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'DatosClinicos')=1 AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idDatoClinico,peso,temperatura,frecuenciaCardiaca,observaciones
    FROM dbo.DatosClinicos WHERE idMascota=@idMascota ORDER BY idDatoClinico DESC;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_DVV_Guardar]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_DVV_ObtenerPorTabla]
    @nombreTabla VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idDVV, nombreTabla, valorDVV
    FROM dbo.DVV
    WHERE nombreTabla = @nombreTabla;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Empresa_Actualizar] @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX),@idEmpresa INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'ModificarEmpresa')=1 AND estado=1 AND bloqueado=0)
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Empresa_Consultar] @idUsuario INT,@idEmpresa INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'Empresas,ConsultaEmpresa,ModificarEmpresa,RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Empresa WHERE idEmpresa=@idEmpresa) THROW 50011, 'No existen datos asociados.', 1;
    SELECT idEmpresa,razonSocial,cuit,direccion,telefono,estado FROM dbo.Empresa WHERE idEmpresa=@idEmpresa;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Empresa_CuitRegistrado] @idUsuario INT,@cuit VARCHAR(20),@idEmpresa INT=0
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaEmpresa,ModificarEmpresa')=1 AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    IF @cuit IS NULL OR LEN(@cuit)<>11 OR @cuit COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9]%' THROW 50025, 'Datos invalidos.', 1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.Empresa WITH(UPDLOCK,HOLDLOCK) WHERE REPLACE(cuit,'-','')=@cuit AND idEmpresa<>@idEmpresa) THEN 1 ELSE 0 END);
END;
GO

-- La BLL guarda DVH, DVV, referencias y auditoria dentro de la transaccion ambiente.
CREATE OR ALTER PROCEDURE [dbo].[sp_Empresa_Listar] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'Empresas,RegistroClienteCorporativo')=1 AND estado=1 AND bloqueado=0)
        THROW 50024, 'Acceso exclusivo para Administrador.', 1;
    IF @@TRANCOUNT=0 THROW 50067, 'Se requiere una transaccion de integridad.', 1;
    SELECT idEmpresa,razonSocial,cuit,direccion,telefono,estado FROM dbo.Empresa ORDER BY razonSocial,idEmpresa;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Empresa_Registrar] @idUsuario INT,@razonSocial NVARCHAR(MAX),@cuit NVARCHAR(MAX),@direccion NVARCHAR(MAX),@telefono NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaEmpresa')=1 AND estado=1 AND bloqueado=0)
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

CREATE OR ALTER PROCEDURE [dbo].[sp_ErrorSistema_ObtenerPorCodigo]
    @codigo VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT codigo, descripcion, criticidad
    FROM dbo.ErrorSistema
    WHERE codigo = @codigo;
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Factura_ListarPropias] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.idSuscripcion,pl.nombre planNombre,p.importe,p.fechaPago,f.numeroFactura
    FROM dbo.Pago p INNER JOIN dbo.Suscripcion s ON s.idSuscripcion=p.idSuscripcion
    INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente INNER JOIN dbo.[Plan] pl ON pl.idPlan=s.idPlan LEFT JOIN dbo.Factura f ON f.idPago=p.idPago
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND p.estado='Aprobado' ORDER BY p.fechaPago DESC,p.idPago DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Factura_Obtener @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT f.*,d.correoFactura FROM dbo.Factura f INNER JOIN dbo.Cliente c ON c.idCliente=f.idCliente
    LEFT JOIN dbo.DetallePago d ON d.idPago=f.idPago
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND f.idSuscripcion=@idSuscripcion;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_HistorialClinico_ConsultarMascota] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50063, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'HistorialClinico')=1 AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idHistorial,fechaRegistro,diagnostico,tratamiento,observaciones
    FROM dbo.HistorialClinico WHERE idMascota=@idMascota ORDER BY fechaRegistro DESC,idHistorial DESC;
END;
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Mascota_ConsultarUsuario] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50061, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Mascotas,ActualizarMascota,ConsultaMascota,HistorialClinico,DatosClinicos,Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idMascota,idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh
    FROM dbo.Mascota WHERE idMascota=@idMascota;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Mascota_ListarUsuario] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50061, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Mascotas,Turnos')=1 AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT idMascota,idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh
    FROM dbo.Mascota WHERE idCliente=@cliente ORDER BY nombre,idMascota;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Mascota_Registrar]
    @idUsuario INT,@nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50060, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Pago_ConsultarPendiente] @idUsuario INT,@idSuscripcion INT
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Pago_ObtenerComprobante] @idUsuario INT,@idSuscripcion INT
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
    SELECT @pago,precio,ROUND(precio*0.8,2),cantidadMascotas,@titular,@correoFactura,'' FROM @resumen;
    SELECT @pago;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_Actualizar] @idUsuario INT,@nombre NVARCHAR(MAX),@descripcion NVARCHAR(MAX),@precio DECIMAL(28,10),@estado BIT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'ModificarPlan')=1 AND estado=1 AND bloqueado=0)
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_AsegurarCatalogo]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_ConsultarAdministracion] @idUsuario INT,@idPlan INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'GestionPlanes,ConsultaPlan,ModificarPlan')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WHERE idPlan=@idPlan) THROW 50011,'No existen datos asociados.',1;
    SELECT idPlan,nombre,descripcion,precio,estado,esCorporativo FROM dbo.[Plan] WHERE idPlan=@idPlan;
    SELECT idCobertura,prestacion,detalle,estado FROM dbo.Cobertura WHERE idPlan=@idPlan ORDER BY prestacion,idCobertura;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_InicializarCorporativo]
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    IF NOT EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK) WHERE esCorporativo=1)
        UPDATE dbo.[Plan] SET esCorporativo=1,precio=COALESCE(precio,16)
        OUTPUT inserted.idPlan WHERE nombre='Plan Corporativo';
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_ListarAdministracion] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'GestionPlanes')=1 AND estado=1 AND bloqueado=0)
        THROW 50024,'Acceso exclusivo para Administrador.',1;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    SELECT idPlan,nombre,descripcion,precio,estado,esCorporativo FROM dbo.[Plan] ORDER BY nombre,idPlan;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_ListarContratables]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE estado=1 AND precio>0 AND esCorporativo=0
    ORDER BY precio,idPlan;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_NecesitaCatalogo]
AS
BEGIN
    SET NOCOUNT ON;
    IF @@TRANCOUNT=0 THROW 50075,'Se requiere una transaccion de integridad.',1;
    SELECT CONVERT(BIT,CASE WHEN EXISTS(SELECT 1 FROM dbo.[Plan] WITH(UPDLOCK,HOLDLOCK)) THEN 0 ELSE 1 END);
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_ObtenerContratable] @idPlan INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idPlan,nombre,descripcion,precio,dvh FROM dbo.[Plan]
    WHERE idPlan=@idPlan AND estado=1 AND precio>0
      AND esCorporativo=0;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Plan_Registrar] @idUsuario INT,@nombre NVARCHAR(MAX),@descripcion NVARCHAR(MAX),@precio DECIMAL(28,10),@estado BIT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF NOT EXISTS(SELECT 1 FROM dbo.Usuario WHERE idUsuario=@idUsuario AND dbo.fn_RolPermite(idRol,'AltaPlan')=1 AND estado=1 AND bloqueado=0)
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

CREATE OR ALTER PROCEDURE [dbo].[sp_ReferenciaIntegridad_EliminarFila]
    @tabla VARCHAR(60),
    @fila VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ReferenciaIntegridad
    WHERE tabla = @tabla AND fila = @fila;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_ReferenciaIntegridad_Guardar]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_ReferenciaIntegridad_Obtener]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tabla, fila, columna, huella
    FROM dbo.ReferenciaIntegridad
    ORDER BY tabla, fila, columna;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Reintegro_ConsultarCliente] @idUsuario INT,@idReintegro INT
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

-- CU-001-005. Consulta de solo lectura; reutiliza Cliente y Reintegro.
CREATE OR ALTER PROCEDURE [dbo].[sp_Reintegro_ListarCliente] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50059, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT idReintegro,fechaSolicitud,importe,estado,detalle FROM dbo.Reintegro
      WHERE idCliente=@cliente ORDER BY fechaSolicitud DESC,idReintegro DESC;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Suscripcion_ActivarPagada] @idUsuario INT,@idSuscripcion INT
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
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Suscripcion_ObtenerPendiente] @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT s.idSuscripcion,s.idPlan FROM dbo.Suscripcion s
    INNER JOIN dbo.Cliente c ON c.idCliente=s.idCliente
    WHERE c.idUsuario=@idUsuario AND c.estado=1 AND s.idSuscripcion=@idSuscripcion AND s.estado='Pendiente';
END;
GO

-- CU-001-004. Reutiliza Cliente, Suscripcion, Plan, Cobertura y la auditoria existente.
CREATE OR ALTER PROCEDURE [dbo].[sp_Suscripcion_ObtenerVigenteCliente] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50058, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Suscripcion_PrepararContratacion]
    @idUsuario INT, @idPlan INT, @versionPlan VARCHAR(64), @cantidadEsperada INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50007, 'Se requiere una transaccion.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c WITH(UPDLOCK,HOLDLOCK)
        INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Turno_ConsultarDisponibilidad] @idUsuario INT,@idMascota INT,@zona VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Turno_ConsultarMascota] @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
        WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario AND c.estado=1 AND dbo.fn_RolPermite(u.idRol,'Turnos')=1 AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    SELECT idTurno,idMascota,fecha,horario,prestacion,zona,estado FROM dbo.Turno WHERE idMascota=@idMascota
    ORDER BY fecha DESC,horario DESC,idTurno DESC;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Turno_Registrar]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Turno_VerificarDisponibilidad]
    @idUsuario INT,@idMascota INT,@idAgendaTurno INT,@fecha DATE,@horario VARCHAR(20),@devolver BIT=1
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ActualizarContrasena] @idUsuario INT,@contrasena VARCHAR(255),@dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50049,'Se requiere una transaccion de integridad.',1;
    UPDATE dbo.Usuario SET contrasena=@contrasena,requiereRestablecerContrasena=0,dvh=@dvh WHERE idUsuario=@idUsuario;
    IF @@ROWCOUNT<>1 THROW 50002,'No existe el usuario indicado.',1;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ActualizarDVH]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ActualizarEstadoSeguridad]
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ActualizarIdioma]
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
    FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
    WHERE idUsuario = @idUsuario;

    IF @@ROWCOUNT = 0
        THROW 50036, 'El usuario seleccionado ya no existe.', 1;

    IF @version IS NULL OR @versionActual IS NULL OR @versionActual <> @version
        THROW 50038, 'Los datos del usuario cambiaron desde su consulta.', 1;

    IF EXISTS (SELECT 1 FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK)
               WHERE correoElectronico = @correo AND idUsuario <> @idUsuario)
        THROW 50001, 'Correo ya registrado.', 1;

    UPDATE dbo.Usuario
    SET nombre = @nombre,
        correoElectronico = @correo,
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_Listar]
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

    SELECT u.idUsuario,u.nombre,u.correoElectronico,u.idRol,p.nombre AS rol,u.estado,u.bloqueado
    FROM dbo.Usuario u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol
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

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ObtenerDvhs]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dvh
    FROM dbo.Usuario
    ORDER BY idUsuario;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ObtenerParaIntegridad]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*,p.nombre AS rol FROM dbo.Usuario u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol ORDER BY idUsuario;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ObtenerPorCorreo] @correoElectronico VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*,p.nombre AS rol FROM dbo.Usuario u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol WHERE correoElectronico=@correoElectronico;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ObtenerPorId] @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*,p.nombre AS rol FROM dbo.Usuario u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol WHERE idUsuario=@idUsuario;
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
    IF EXISTS (SELECT 1 FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK) WHERE correoElectronico=@correo)
        THROW 50001, 'Correo ya registrado.', 1;
    INSERT dbo.Usuario(nombre, correoElectronico, contrasena, idRol, estado,
        intentosFallidos, bloqueado, idioma, dvh)
    OUTPUT INSERTED.idUsuario
    VALUES(@nombre, @correo, @contrasena, @idRol, 1, 0, 0, 'es', '');
END;
GO
