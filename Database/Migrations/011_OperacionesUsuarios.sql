-- CU-007-005 a CU-007-008. No cambia datos de usuarios ni recalcula integridad existente.
INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
SELECT codigo, descripcion, criticidad FROM (VALUES
 ('ER036', 'El usuario seleccionado ya no existe', 'Advertencia'),
 ('ER037', 'El estado actual del usuario no permite realizar la operación', 'Advertencia'),
 ('ER038', 'Los datos del usuario cambiaron desde su consulta', 'Advertencia'),
 ('ER039', 'No se pudo actualizar el usuario', 'Grave'),
 ('ER040', 'No se pudo registrar la operación en bitácora', 'Grave')
) AS nuevos(codigo, descripcion, criticidad)
WHERE NOT EXISTS (SELECT 1 FROM dbo.ErrorSistema e WHERE e.codigo = nuevos.codigo);
GO
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ObtenerPorId @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idUsuario, nombre, correoElectronico, rol, estado, bloqueado,
           intentosFallidos, idioma, contrasena, dvh
    FROM dbo.Usuario WHERE idUsuario = @idUsuario;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_GuardarAdministracion
    @idUsuario INT, @version VARCHAR(64), @nombre VARCHAR(100), @correo VARCHAR(100),
    @rol VARCHAR(30), @estado BIT, @bloqueado BIT, @intentos TINYINT, @dvh VARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT = 0 THROW 50039, 'Se requiere una transacción.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK) WHERE idUsuario = @idUsuario)
        THROW 50036, 'El usuario seleccionado ya no existe.', 1;
    IF NOT EXISTS (SELECT 1 FROM dbo.Usuario WHERE idUsuario = @idUsuario AND dvh = @version)
        THROW 50038, 'Los datos del usuario cambiaron desde su consulta.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK) WHERE correoElectronico = @correo AND idUsuario <> @idUsuario)
        THROW 50001, 'Correo ya registrado.', 1;
    UPDATE dbo.Usuario SET nombre=@nombre, correoElectronico=@correo, rol=@rol,
        estado=@estado, bloqueado=@bloqueado, intentosFallidos=@intentos, dvh=@dvh
    WHERE idUsuario=@idUsuario AND dvh=@version;
    IF @@ROWCOUNT <> 1 THROW 50039, 'No se pudo actualizar el usuario.', 1;
END;
