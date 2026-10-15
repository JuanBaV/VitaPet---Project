-- CU-007-004: ER028 belongs to the approved users specification.
-- Preserve password recovery under its own code without editing prior migrations.
IF NOT EXISTS (SELECT 1 FROM dbo.ErrorSistema WHERE codigo = 'ER035')
    INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
    VALUES ('ER035', 'Correo electrónico o respuesta incorrectos', 'Advertencia');
IF EXISTS (SELECT 1 FROM dbo.ErrorSistema WHERE codigo = 'ER028')
    UPDATE dbo.ErrorSistema SET descripcion = 'No se encontraron usuarios que coincidan con los parámetros de filtrado.', criticidad = 'Advertencia' WHERE codigo = 'ER028';
ELSE
    INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
    VALUES ('ER028', 'No se encontraron usuarios que coincidan con los parámetros de filtrado.', 'Advertencia');
GO
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_Listar
    @nombre NVARCHAR(100) = N'',
    @estado INT = 0,
    @hayUsuarios BIT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    IF @estado NOT BETWEEN 0 AND 4 OR @estado IS NULL
        THROW 50001, 'Estado de filtro inválido.', 1;
    SET @hayUsuarios = CASE WHEN EXISTS (SELECT 1 FROM dbo.Usuario) THEN 1 ELSE 0 END;
    SELECT idUsuario, nombre, correoElectronico, rol, estado, bloqueado
    FROM dbo.Usuario
    WHERE (NULLIF(LTRIM(RTRIM(@nombre)), N'') IS NULL
           OR CHARINDEX(LTRIM(RTRIM(@nombre)) COLLATE Latin1_General_CI_AI, nombre COLLATE Latin1_General_CI_AI) > 0)
      AND (@estado = 0 OR (@estado = 1 AND estado = 1) OR (@estado = 2 AND estado = 0)
           OR (@estado = 3 AND bloqueado = 1) OR (@estado = 4 AND bloqueado = 0))
    ORDER BY nombre, idUsuario;
END;
