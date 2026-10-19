-- CU-003-001/002: agenda local por zona y prestación. No modifica filas protegidas existentes.
IF OBJECT_ID(N'dbo.AgendaTurno',N'U') IS NOT NULL
    THROW 50065, 'AgendaTurno ya existe fuera de la migracion esperada.', 1;
CREATE TABLE dbo.AgendaTurno
(
    idAgendaTurno INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AgendaTurno PRIMARY KEY,
    zona VARCHAR(60) NOT NULL,
    prestacion VARCHAR(100) NOT NULL,
    horaInicio TIME(0) NOT NULL,
    horaFin TIME(0) NOT NULL,
    duracionMinutos SMALLINT NOT NULL,
    horizonteDias SMALLINT NOT NULL,
    estado BIT NOT NULL,
    dvh VARCHAR(64) NOT NULL,
    CONSTRAINT UQ_AgendaTurno_ZonaPrestacion UNIQUE(zona,prestacion),
    CONSTRAINT CK_AgendaTurno_Configuracion CHECK(horaFin>horaInicio AND duracionMinutos BETWEEN 15 AND 120 AND horizonteDias BETWEEN 1 AND 90)
);
CREATE UNIQUE INDEX UX_Turno_Confirmado ON dbo.Turno(zona,prestacion,fecha,horario) WHERE estado='Confirmado';
CREATE INDEX IX_Turno_Mascota ON dbo.Turno(idMascota,fecha DESC,horario DESC,idTurno DESC);
INSERT dbo.DVV(nombreTabla,valorDVV)
VALUES('AgendaTurno',LOWER(CONVERT(VARCHAR(64),HASHBYTES('SHA2_256',CONVERT(VARCHAR(MAX),'')),2)));
GO
CREATE OR ALTER PROCEDURE dbo.sp_AgendaTurno_Inicializar
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    -- Configuración de demostración aprobada. Una agenda existente nunca se reemplaza.
    IF NOT EXISTS(SELECT 1 FROM dbo.AgendaTurno WITH(UPDLOCK,HOLDLOCK))
        INSERT dbo.AgendaTurno(zona,prestacion,horaInicio,horaFin,duracionMinutos,horizonteDias,estado,dvh)
        OUTPUT inserted.idAgendaTurno
        SELECT z.zona,p.prestacion,'09:00','18:00',30,30,1,''
        FROM (VALUES('CABA'),('Norte'),('Sur'),('Oeste')) z(zona)
        CROSS JOIN (VALUES('Consulta general'),('Vacunación'),('Control preventivo')) p(prestacion);
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_AgendaTurno_ListarZonas @idUsuario INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Cliente c JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
        WHERE c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    SELECT DISTINCT zona FROM dbo.AgendaTurno WHERE estado=1 ORDER BY zona;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Turno_ConsultarDisponibilidad @idUsuario INT,@idMascota INT,@zona VARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
        WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    IF NULLIF(LTRIM(RTRIM(@zona)),'') IS NULL THROW 50002, 'Datos incompletos.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.AgendaTurno WHERE zona=@zona AND estado=1) THROW 50025, 'Datos invalidos.', 1;
    DECLARE @ahora DATETIME=GETDATE(), @hoy DATE=CONVERT(DATE,GETDATE());
    ;WITH digito(n) AS (SELECT n FROM(VALUES(0),(1),(2),(3),(4),(5),(6),(7),(8),(9))x(n)),
    numero(n) AS (SELECT a.n+10*b.n FROM digito a CROSS JOIN digito b)
    SELECT a.idAgendaTurno,a.prestacion,f.fecha,CONVERT(VARCHAR(5),h.hora,108) AS horario,a.zona
    FROM dbo.AgendaTurno a CROSS JOIN numero dias CROSS JOIN numero bloques
    CROSS APPLY(SELECT DATEADD(DAY,dias.n,@hoy) AS fecha) f
    CROSS APPLY(SELECT DATEADD(MINUTE,bloques.n*a.duracionMinutos,a.horaInicio) AS hora) h
    WHERE a.estado=1 AND a.zona=@zona AND dias.n<a.horizonteDias
      AND DATEDIFF(DAY,CONVERT(DATE,'19000101'),f.fecha)%7 BETWEEN 0 AND 5
      AND bloques.n*a.duracionMinutos+a.duracionMinutos<=DATEDIFF(MINUTE,a.horaInicio,a.horaFin)
      AND DATEADD(MINUTE,DATEDIFF(MINUTE,CONVERT(TIME,'00:00'),h.hora),CONVERT(DATETIME,f.fecha))>@ahora
      AND NOT EXISTS(SELECT 1 FROM dbo.Turno t WHERE t.zona=a.zona AND t.prestacion=a.prestacion
          AND t.fecha=f.fecha AND t.horario=CONVERT(VARCHAR(5),h.hora,108) AND t.estado='Confirmado')
    ORDER BY f.fecha,h.hora,a.prestacion;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Turno_VerificarDisponibilidad
    @idUsuario INT,@idMascota INT,@idAgendaTurno INT,@fecha DATE,@horario VARCHAR(20),@devolver BIT=1
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
        WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    IF @idAgendaTurno IS NULL OR @fecha IS NULL OR NULLIF(@horario,'') IS NULL THROW 50002, 'Datos incompletos.', 1;
    DECLARE @hora TIME(0)=TRY_CONVERT(TIME(0),@horario),@ahora DATETIME=GETDATE(),@hoy DATE=CONVERT(DATE,GETDATE());
    IF @hora IS NULL OR @horario<>CONVERT(VARCHAR(5),@hora,108) THROW 50025, 'Datos invalidos.', 1;
    DECLARE @zona VARCHAR(60),@prestacion VARCHAR(100),@inicio TIME(0),@fin TIME(0),@duracion INT,@horizonte INT;
    SELECT @zona=zona,@prestacion=prestacion,@inicio=horaInicio,@fin=horaFin,@duracion=duracionMinutos,@horizonte=horizonteDias
    FROM dbo.AgendaTurno WHERE idAgendaTurno=@idAgendaTurno AND estado=1;
    IF @zona IS NULL THROW 50013, 'El turno ya no se encuentra disponible.', 1;
    IF @fecha<@hoy OR DATEADD(MINUTE,DATEDIFF(MINUTE,CONVERT(TIME,'00:00'),@hora),CONVERT(DATETIME,@fecha))<=@ahora
        THROW 50013, 'El turno ya no se encuentra disponible.', 1;
    IF @fecha>=DATEADD(DAY,@horizonte,@hoy) OR DATEDIFF(DAY,CONVERT(DATE,'19000101'),@fecha)%7 NOT BETWEEN 0 AND 5
      OR @hora<@inicio OR DATEDIFF(MINUTE,@inicio,@hora)%@duracion<>0
      OR DATEDIFF(MINUTE,@inicio,@hora)+@duracion>DATEDIFF(MINUTE,@inicio,@fin)
        THROW 50025, 'Datos invalidos.', 1;
    IF EXISTS(SELECT 1 FROM dbo.Turno WITH(UPDLOCK,HOLDLOCK) WHERE zona=@zona AND prestacion=@prestacion
        AND fecha=@fecha AND horario=@horario AND estado='Confirmado')
        THROW 50013, 'El turno ya no se encuentra disponible.', 1;
    IF @devolver=1 SELECT @idAgendaTurno AS idAgendaTurno,@prestacion AS prestacion,@fecha AS fecha,@horario AS horario,@zona AS zona;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Turno_Registrar
    @idUsuario INT,@idMascota INT,@idAgendaTurno INT,@fecha DATE,@horario VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50066, 'Se requiere una transaccion de integridad.', 1;
    EXEC dbo.sp_Turno_VerificarDisponibilidad @idUsuario,@idMascota,@idAgendaTurno,@fecha,@horario,0;
    BEGIN TRY
        INSERT dbo.Turno(idMascota,fecha,horario,prestacion,zona,estado,dvh)
        OUTPUT inserted.idTurno,inserted.idMascota,inserted.fecha,inserted.horario,inserted.prestacion,inserted.zona,inserted.estado
        SELECT @idMascota,@fecha,@horario,prestacion,zona,'Confirmado','' FROM dbo.AgendaTurno WHERE idAgendaTurno=@idAgendaTurno;
    END TRY
    BEGIN CATCH
        IF ERROR_NUMBER() IN(2601,2627) THROW 50013, 'El turno ya no se encuentra disponible.', 1;
        THROW;
    END CATCH;
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Turno_ConsultarMascota @idUsuario INT,@idMascota INT
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON;
    IF @@TRANCOUNT=0 THROW 50065, 'Se requiere una transaccion de integridad.', 1;
    IF NOT EXISTS(SELECT 1 FROM dbo.Mascota m JOIN dbo.Cliente c ON c.idCliente=m.idCliente JOIN dbo.Usuario u ON u.idUsuario=c.idUsuario
        WHERE m.idMascota=@idMascota AND c.idUsuario=@idUsuario AND c.estado=1 AND u.rol='Cliente' AND u.estado=1 AND u.bloqueado=0)
        THROW 50011, 'No existen datos asociados.', 1;
    SELECT idTurno,idMascota,fecha,horario,prestacion,zona,estado FROM dbo.Turno WHERE idMascota=@idMascota
    ORDER BY fecha DESC,horario DESC,idTurno DESC;
END;
