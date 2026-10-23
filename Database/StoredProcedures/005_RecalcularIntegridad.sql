USE [VitaPet]
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

UPDATE dbo.Usuario
SET dvh = LOWER(CONVERT(VARCHAR(64), HASHBYTES(
    'SHA2_256',
    CONVERT(VARCHAR(MAX),
        CONCAT(
            LEN(CONVERT(VARCHAR(20), idUsuario)), ':', idUsuario,
            LEN(correoElectronico), ':', correoElectronico,
            LEN(contrasena), ':', contrasena,
            LEN(CONVERT(VARCHAR(20), idRol)), ':', idRol,
            1, ':', CASE WHEN estado = 1 THEN '1' ELSE '0' END,
            LEN(nombre), ':', nombre,
            LEN(CONVERT(VARCHAR(3), intentosFallidos)), ':', intentosFallidos,
            1, ':', CASE WHEN bloqueado = 1 THEN '1' ELSE '0' END,
            LEN(idioma), ':', idioma,
            CASE WHEN requiereRestablecerContrasena=1 THEN '29:requiereRestablecerContrasena1:1' ELSE '' END
        ) COLLATE Latin1_General_100_BIN2_UTF8
    )
), 2));

UPDATE dbo.Bitacora
SET dvh = LOWER(CONVERT(VARCHAR(64), HASHBYTES(
    'SHA2_256',
    CONVERT(VARCHAR(MAX),
        CONCAT(
            CASE WHEN idUsuario IS NULL THEN 0 ELSE LEN(CONVERT(VARCHAR(20), idUsuario)) END,
            ':', COALESCE(CONVERT(VARCHAR(20), idUsuario), ''),
            14, ':', CONVERT(CHAR(8), fecha, 112), REPLACE(CONVERT(CHAR(8), fecha, 108), ':', ''),
            LEN(accion), ':', accion,
            LEN(modulo), ':', modulo,
            LEN(criticidad), ':', criticidad
        ) COLLATE Latin1_General_100_BIN2_UTF8
    )
), 2));

DECLARE @dvvUsuario VARCHAR(64);
DECLARE @dvvBitacora VARCHAR(64);

SELECT @dvvUsuario = LOWER(CONVERT(VARCHAR(64), HASHBYTES(
    'SHA2_256',
    CONVERT(VARCHAR(MAX), COALESCE(
        STRING_AGG(CONVERT(VARCHAR(MAX), CONCAT(LEN(dvh), ':', dvh)), '')
            WITHIN GROUP (ORDER BY idUsuario),
        ''
    )) COLLATE Latin1_General_100_BIN2_UTF8
), 2))
FROM dbo.Usuario;

SELECT @dvvBitacora = LOWER(CONVERT(VARCHAR(64), HASHBYTES(
    'SHA2_256',
    CONVERT(VARCHAR(MAX), COALESCE(
        STRING_AGG(CONVERT(VARCHAR(MAX), CONCAT(LEN(dvh), ':', dvh)), '')
            WITHIN GROUP (ORDER BY idBitacora),
        ''
    )) COLLATE Latin1_General_100_BIN2_UTF8
), 2))
FROM dbo.Bitacora;

UPDATE dbo.DVV
SET valorDVV = CASE nombreTabla
    WHEN 'Usuario' THEN @dvvUsuario
    WHEN 'Bitacora' THEN @dvvBitacora
END
WHERE nombreTabla IN ('Usuario', 'Bitacora');

COMMIT TRANSACTION;
GO
