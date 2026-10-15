-- CU-002-001. Reutiliza Cliente, Mascota y los controles de integridad existentes.
CREATE OR ALTER PROCEDURE dbo.sp_Cliente_ObtenerActivoUsuario @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50060, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT @cliente idCliente;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Mascota_Registrar
    @idUsuario INT,@nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50060, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c INNER JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),'') IS NULL OR NULLIF(LTRIM(RTRIM(@especie)),'') IS NULL
      OR NULLIF(LTRIM(RTRIM(@raza)),'') IS NULL OR @fechaNacimiento IS NULL
      THROW 50002, 'Datos incompletos.', 1;
    IF @fechaNacimiento>CONVERT(DATE,GETDATE()) THROW 50025, 'Datos invalidos.', 1;
    INSERT dbo.Mascota(idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh)
      VALUES(@cliente,LTRIM(RTRIM(@nombre)),LTRIM(RTRIM(@especie)),LTRIM(RTRIM(@raza)),@fechaNacimiento,1,'');
    SELECT CONVERT(INT,SCOPE_IDENTITY()) idMascota;
END;
