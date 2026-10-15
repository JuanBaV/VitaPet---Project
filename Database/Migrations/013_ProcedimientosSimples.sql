-- Definiciones vigentes. Conserva parámetros, resultados y errores públicos.
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_Eliminar
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

CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_ObtenerDvhs
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dvh
    FROM dbo.Bitacora
    ORDER BY idBitacora;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_ObtenerExcedentes
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

CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_Registrar
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

CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_ObtenerParaIntegridad
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh
    FROM dbo.Bitacora
    ORDER BY idBitacora;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_ActualizarDVH
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

CREATE OR ALTER PROCEDURE dbo.sp_DVV_Guardar
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

CREATE OR ALTER PROCEDURE dbo.sp_DVV_ObtenerPorTabla
    @nombreTabla VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idDVV, nombreTabla, valorDVV
    FROM dbo.DVV
    WHERE nombreTabla = @nombreTabla;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ErrorSistema_ObtenerPorCodigo
    @codigo VARCHAR(10)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT codigo, descripcion, criticidad
    FROM dbo.ErrorSistema
    WHERE codigo = @codigo;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ActualizarContrasena
    @idUsuario INT,
    @contrasena VARCHAR(255),
    @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.Usuario
    SET contrasena = @contrasena,
        dvh = @dvh
    WHERE idUsuario = @idUsuario;

    IF @@ROWCOUNT = 0
        THROW 50002, 'No existe el usuario indicado.', 1;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ActualizarEstadoSeguridad
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

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerDvhs
AS
BEGIN
    SET NOCOUNT ON;

    SELECT dvh
    FROM dbo.Usuario
    ORDER BY idUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerPorCorreo
    @correoElectronico VARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idUsuario, correoElectronico, contrasena, nombre, rol, estado,
           intentosFallidos, bloqueado, idioma, dvh
    FROM dbo.Usuario
    WHERE correoElectronico = @correoElectronico;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerParaIntegridad
AS
BEGIN
    SET NOCOUNT ON;

    SELECT idUsuario, correoElectronico, contrasena, nombre, rol, estado,
           intentosFallidos, bloqueado, idioma, dvh
    FROM dbo.Usuario
    ORDER BY idUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ActualizarDVH
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

CREATE OR ALTER PROCEDURE dbo.sp_ReferenciaIntegridad_Obtener
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tabla, fila, columna, huella
    FROM dbo.ReferenciaIntegridad
    ORDER BY tabla, fila, columna;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_ReferenciaIntegridad_Guardar
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

CREATE OR ALTER PROCEDURE dbo.sp_ReferenciaIntegridad_EliminarFila
    @tabla VARCHAR(60),
    @fila VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM dbo.ReferenciaIntegridad
    WHERE tabla = @tabla AND fila = @fila;
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

    SET @hayUsuarios = 0;
    IF EXISTS (SELECT 1 FROM dbo.Usuario)
        SET @hayUsuarios = 1;

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

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerPorId
    @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idUsuario, nombre, correoElectronico, rol, estado, bloqueado,
           intentosFallidos, idioma, contrasena, dvh
    FROM dbo.Usuario
    WHERE idUsuario = @idUsuario;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Usuario_GuardarAdministracion
    @idUsuario INT,
    @version VARCHAR(64),
    @nombre VARCHAR(100),
    @correo VARCHAR(100),
    @rol VARCHAR(30),
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
        rol = @rol,
        estado = @estado,
        bloqueado = @bloqueado,
        intentosFallidos = @intentos,
        dvh = @dvh
    WHERE idUsuario = @idUsuario AND dvh = @version;

    IF @@ROWCOUNT <> 1
        THROW 50039, 'No se pudo actualizar el usuario.', 1;
END;
GO
