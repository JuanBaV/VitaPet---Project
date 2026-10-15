-- CU-001-005. Consulta de solo lectura; reutiliza Cliente y Reintegro.
CREATE OR ALTER PROCEDURE dbo.sp_Reintegro_ListarCliente @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50059, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT idReintegro,fechaSolicitud,importe,estado,detalle FROM dbo.Reintegro
      WHERE idCliente=@cliente ORDER BY fechaSolicitud DESC,idReintegro DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Reintegro_ConsultarCliente @idUsuario INT,@idReintegro INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50059, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Reintegro WHERE idReintegro=@idReintegro AND idCliente=@cliente)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idReintegro,fechaSolicitud,importe,estado,detalle FROM dbo.Reintegro
      WHERE idReintegro=@idReintegro AND idCliente=@cliente;
END;
