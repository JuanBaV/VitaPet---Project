SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
ALTER TABLE dbo.Bitacora ADD archivado BIT NOT NULL
    CONSTRAINT DF_Bitacora_Archivado DEFAULT (0) WITH VALUES;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_Eliminar
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
CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_ObtenerParaIntegridad
AS
BEGIN
    SET NOCOUNT ON;
    SELECT idBitacora, idUsuario, fecha, accion, modulo, criticidad, dvh, archivado
    FROM dbo.Bitacora ORDER BY idBitacora;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Bitacora_ObtenerExcedentes
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
