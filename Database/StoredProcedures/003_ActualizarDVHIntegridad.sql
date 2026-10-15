USE [VitaPet]
GO

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
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
