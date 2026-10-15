CREATE TABLE dbo.ReferenciaIntegridad
(
    tabla varchar(60) NOT NULL,
    fila varchar(30) NOT NULL,
    columna varchar(60) NOT NULL,
    huella char(64) NOT NULL,
    CONSTRAINT PK_ReferenciaIntegridad PRIMARY KEY (tabla, fila, columna)
);
GO
CREATE PROCEDURE dbo.sp_ReferenciaIntegridad_Obtener
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tabla, fila, columna, huella FROM dbo.ReferenciaIntegridad ORDER BY tabla, fila, columna;
END;
GO
CREATE PROCEDURE dbo.sp_ReferenciaIntegridad_Guardar
    @tabla varchar(60), @fila varchar(30), @columna varchar(60), @huella char(64)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.ReferenciaIntegridad SET huella=@huella WHERE tabla=@tabla AND fila=@fila AND columna=@columna;
    IF @@ROWCOUNT=0 INSERT dbo.ReferenciaIntegridad(tabla,fila,columna,huella) VALUES(@tabla,@fila,@columna,@huella);
END;
GO
CREATE PROCEDURE dbo.sp_ReferenciaIntegridad_EliminarFila @tabla varchar(60), @fila varchar(30)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE dbo.ReferenciaIntegridad WHERE tabla=@tabla AND fila=@fila;
END;
