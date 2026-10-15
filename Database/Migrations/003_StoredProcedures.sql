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
    BEGIN
        INSERT INTO dbo.DVV (nombreTabla, valorDVV)
        VALUES (@nombreTabla, @valorDVV);
    END;
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
