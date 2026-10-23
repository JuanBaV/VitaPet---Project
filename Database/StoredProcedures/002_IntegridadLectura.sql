USE [VitaPet]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_Usuario_ObtenerParaIntegridad]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT u.*,p.nombre AS rol FROM dbo.Usuario u JOIN dbo.Rol r ON r.idRol=u.idRol JOIN dbo.PermisoComponente p ON p.idComponente=r.idRol ORDER BY idUsuario;
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