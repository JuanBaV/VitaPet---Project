CREATE OR ALTER PROCEDURE dbo.sp_Usuario_ActualizarIdioma
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
