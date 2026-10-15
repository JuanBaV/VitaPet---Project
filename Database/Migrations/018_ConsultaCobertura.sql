-- CU-001-004. Reutiliza Cliente, Suscripcion, Plan, Cobertura y la auditoria existente.
CREATE OR ALTER PROCEDURE dbo.sp_Suscripcion_ObtenerVigenteCliente @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50058, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    DECLARE @hoy DATE=CONVERT(DATE,GETDATE());
    IF NOT EXISTS(SELECT 1 FROM dbo.Suscripcion WHERE idCliente=@cliente AND estado='Activa'
      AND fechaAlta<=@hoy AND fechaVencimiento>=@hoy)
      THROW 50008, 'No existe una cobertura asociada.', 1;
    SELECT TOP(1) s.idSuscripcion,s.idPlan,p.nombre,p.descripcion,s.estado,s.fechaAlta,s.fechaVencimiento
      FROM dbo.Suscripcion s INNER JOIN dbo.[Plan] p ON p.idPlan=s.idPlan
      WHERE s.idCliente=@cliente AND s.estado='Activa' AND s.fechaAlta<=@hoy AND s.fechaVencimiento>=@hoy
      ORDER BY s.fechaAlta DESC,s.idSuscripcion DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Cobertura_ConsultarCuenta @idUsuario INT,@idSuscripcion INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50058, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT idCliente FROM dbo.Cliente WHERE idUsuario=@idUsuario AND estado=1);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    DECLARE @plan INT,@hoy DATE=CONVERT(DATE,GETDATE());
    SELECT @plan=idPlan FROM dbo.Suscripcion WHERE idSuscripcion=@idSuscripcion AND idCliente=@cliente
      AND estado='Activa' AND fechaAlta<=@hoy AND fechaVencimiento>=@hoy;
    IF @plan IS NULL THROW 50008, 'No existe una cobertura asociada.', 1;
    SELECT prestacion,detalle FROM dbo.Cobertura WHERE idPlan=@plan AND estado=1 ORDER BY idCobertura;
END;
