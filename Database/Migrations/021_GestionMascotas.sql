-- CU-002-002/003/004 y CU-004-001/002. Las tablas existentes conservan sus filas y verificadores.
IF OBJECT_ID(N'dbo.DatosClinicos', N'U') IS NOT NULL
    THROW 50063, 'DatosClinicos ya existe fuera de la migracion esperada.', 1;
CREATE TABLE dbo.DatosClinicos
(
    idDatoClinico INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DatosClinicos PRIMARY KEY,
    idMascota INT NOT NULL,
    peso DECIMAL(6,2) NOT NULL,
    temperatura DECIMAL(5,2) NOT NULL,
    frecuenciaCardiaca INT NOT NULL,
    observaciones VARCHAR(255) NULL,
    dvh VARCHAR(64) NOT NULL,
    CONSTRAINT FK_DatosClinicos_Mascota FOREIGN KEY(idMascota) REFERENCES dbo.Mascota(idMascota),
    CONSTRAINT CK_DatosClinicos_Valores CHECK(peso>0 AND temperatura>0 AND frecuenciaCardiaca>0)
);
CREATE INDEX IX_DatosClinicos_Mascota ON dbo.DatosClinicos(idMascota,idDatoClinico);
CREATE INDEX IX_HistorialClinico_Mascota ON dbo.HistorialClinico(idMascota,fechaRegistro,idHistorial);
-- SHA256 del conjunto vacío. No recalcula ni repara ninguna fila o referencia previa.
INSERT dbo.DVV(nombreTabla,valorDVV)
VALUES('DatosClinicos',LOWER(CONVERT(VARCHAR(64),HASHBYTES('SHA2_256',CONVERT(VARCHAR(MAX),'')),2)));
GO
CREATE OR ALTER PROCEDURE dbo.sp_Mascota_ListarUsuario @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50061, 'Se requiere una transaccion de integridad.', 1;
    DECLARE @cliente INT=(SELECT c.idCliente FROM dbo.Cliente c JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
      WHERE c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0);
    IF @cliente IS NULL THROW 50011, 'No existen datos asociados.', 1;
    SELECT idMascota,idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh
    FROM dbo.Mascota WHERE idCliente=@cliente ORDER BY nombre,idMascota;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Mascota_ConsultarUsuario @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50061, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idMascota,idCliente,nombre,especie,raza,fechaNacimiento,estado,dvh
    FROM dbo.Mascota WHERE idMascota=@idMascota;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Mascota_Actualizar
 @idUsuario INT,@idMascota INT,@dvhOriginal VARCHAR(64),
 @nombre VARCHAR(60),@especie VARCHAR(40),@raza VARCHAR(60),@fechaNacimiento DATE
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50062, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    IF NULLIF(LTRIM(RTRIM(@nombre)),'') IS NULL OR NULLIF(LTRIM(RTRIM(@especie)),'') IS NULL
      OR NULLIF(LTRIM(RTRIM(@raza)),'') IS NULL OR @fechaNacimiento IS NULL
      THROW 50002, 'Datos incompletos.', 1;
    IF @fechaNacimiento>CONVERT(DATE,GETDATE()) THROW 50025, 'Datos invalidos.', 1;
    IF @dvhOriginal IS NULL OR NOT EXISTS(SELECT 1 FROM dbo.Mascota WHERE idMascota=@idMascota AND dvh=@dvhOriginal)
      THROW 50064, 'Los datos de la mascota cambiaron desde su consulta.', 1;
    UPDATE dbo.Mascota SET nombre=LTRIM(RTRIM(@nombre)),especie=LTRIM(RTRIM(@especie)),
      raza=LTRIM(RTRIM(@raza)),fechaNacimiento=@fechaNacimiento WHERE idMascota=@idMascota;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_HistorialClinico_ConsultarMascota @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50063, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idHistorial,fechaRegistro,diagnostico,tratamiento,observaciones
    FROM dbo.HistorialClinico WHERE idMascota=@idMascota ORDER BY fechaRegistro DESC,idHistorial DESC;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_DatosClinicos_ConsultarMascota @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50063, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente
      JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario
      AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
      THROW 50011, 'No existen datos asociados.', 1;
    SELECT idDatoClinico,peso,temperatura,frecuenciaCardiaca,observaciones
    FROM dbo.DatosClinicos WHERE idMascota=@idMascota ORDER BY idDatoClinico DESC;
END;
