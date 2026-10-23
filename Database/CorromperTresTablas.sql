
USE [VitaPet];
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'VitaPet'
    THROW 51000, 'Este script solo se ejecuta sobre la base VitaPet.', 1;
IF @@TRANCOUNT <> 0
    THROW 51001, 'Ejecutar sin una transaccion previa abierta.', 1;

DROP TABLE IF EXISTS #Candidatas;
DROP TABLE IF EXISTS #Filas;
DROP TABLE IF EXISTS #Cambios;

CREATE TABLE #Candidatas
(
    Tabla SYSNAME PRIMARY KEY,
    Clave SYSNAME NOT NULL,
    Campo SYSNAME NOT NULL,
    Cantidad INT NOT NULL DEFAULT 0
);
INSERT #Candidatas(Tabla, Clave, Campo) VALUES
('Empresa','idEmpresa','direccion'),
('Cliente','idCliente','apellido'),
('Plan','idPlan','descripcion'),
('Cobertura','idCobertura','detalle'),
('Suscripcion','idSuscripcion','estado'),
('Pago','idPago','medioPago'),
('Mascota','idMascota','nombre'),
('Turno','idTurno','prestacion'),
('HistorialClinico','idHistorial','observaciones'),
('Reintegro','idReintegro','detalle'),
('ErrorSistema','codigo','descripcion'),
('DatosClinicos','idDatoClinico','observaciones'),
('AgendaTurno','idAgendaTurno','prestacion'),
('DetallePago','idPago','titular'),
('Factura','idFactura','concepto'),
('Usuario','idUsuario','nombre'),
('Bitacora','idBitacora','accion');

CREATE TABLE #Filas(Clave NVARCHAR(255) PRIMARY KEY);
CREATE TABLE #Cambios
(
    Tabla SYSNAME NOT NULL,
    Clave NVARCHAR(255) NOT NULL,
    Campo SYSNAME NOT NULL,
    ValorOriginal NVARCHAR(MAX) NULL,
    ValorAlterado NVARCHAR(MAX) NOT NULL,
    DvhOriginal VARCHAR(64) NOT NULL,
    DvhPosterior VARCHAR(64) NOT NULL
);

DECLARE @tabla SYSNAME, @clave SYSNAME, @campo SYSNAME,
        @sql NVARCHAR(MAX), @filtro NVARCHAR(100), @cantidad INT,
        @marca VARCHAR(40);
-- La marca por ejecucion y la clave mantienen distintos los valores con indice unico.
SET @marca = 'P4_' + LEFT(REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', ''), 12);

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @bloqueo INT;
    EXEC @bloqueo = sys.sp_getapplock
        @Resource=N'VitaPet.Integridad', @LockMode='Exclusive',
        @LockOwner='Transaction', @LockTimeout=30000;
    IF @bloqueo < 0 THROW 51002, 'No se pudo bloquear la prueba de integridad.', 1;

    DECLARE tablas CURSOR LOCAL FAST_FORWARD FOR
        SELECT Tabla, Clave, Campo FROM #Candidatas;
    OPEN tablas;
    FETCH NEXT FROM tablas INTO @tabla, @clave, @campo;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        IF OBJECT_ID(N'dbo.' + QUOTENAME(@tabla), N'U') IS NULL
            THROW 51003, 'Falta una tabla del esquema esperado de VitaPet.', 1;
        SET @filtro = CASE WHEN @tabla='Usuario' THEN N' WHERE idRol NOT IN(SELECT idComponente FROM dbo.PermisoComponente WHERE nombre=''Webmaster'' AND sistema=1)' ELSE N'' END;
        SET @sql = N'SELECT @total=COUNT(*) FROM dbo.'+QUOTENAME(@tabla)+
            N' WITH (UPDLOCK,HOLDLOCK)'+@filtro+N';';
        EXEC sys.sp_executesql @sql, N'@total INT OUTPUT', @total=@cantidad OUTPUT;
        UPDATE #Candidatas SET Cantidad=@cantidad WHERE Tabla=@tabla;
        FETCH NEXT FROM tablas INTO @tabla, @clave, @campo;
    END;
    CLOSE tablas;
    DEALLOCATE tablas;

    IF (SELECT COUNT(*) FROM #Candidatas WHERE Cantidad>=3)<3
        THROW 51004, 'Se necesitan al menos 3 tablas con 3 registros elegibles cada una.', 1;

    DECLARE elegidas CURSOR LOCAL FAST_FORWARD FOR
        SELECT TOP(3) Tabla, Clave, Campo FROM #Candidatas WHERE Cantidad>=3 ORDER BY NEWID();
    OPEN elegidas;
    FETCH NEXT FROM elegidas INTO @tabla, @clave, @campo;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        DELETE FROM #Filas;
        SET @filtro = CASE WHEN @tabla='Usuario' THEN N' WHERE idRol NOT IN(SELECT idComponente FROM dbo.PermisoComponente WHERE nombre=''Webmaster'' AND sistema=1)' ELSE N'' END;
        SET @sql = N'INSERT #Filas(Clave) SELECT TOP(3) CONVERT(NVARCHAR(255),'+QUOTENAME(@clave)+
            N') FROM dbo.'+QUOTENAME(@tabla)+N' WITH (UPDLOCK,HOLDLOCK)'+@filtro+N' ORDER BY NEWID();
            UPDATE t SET '+QUOTENAME(@campo)+N'=@marca+''_''+CONVERT(VARCHAR(20),t.'+QUOTENAME(@clave)+N')
            OUTPUT @tabla,CONVERT(NVARCHAR(255),deleted.'+QUOTENAME(@clave)+N'),@campo,
                CONVERT(NVARCHAR(MAX),deleted.'+QUOTENAME(@campo)+N'),
                CONVERT(NVARCHAR(MAX),inserted.'+QUOTENAME(@campo)+N'),deleted.dvh,inserted.dvh
            INTO #Cambios(Tabla,Clave,Campo,ValorOriginal,ValorAlterado,DvhOriginal,DvhPosterior)
            FROM dbo.'+QUOTENAME(@tabla)+N' t JOIN #Filas f ON f.Clave=CONVERT(NVARCHAR(255),t.'+QUOTENAME(@clave)+N');';
        EXEC sys.sp_executesql @sql,
            N'@marca VARCHAR(40),@tabla SYSNAME,@campo SYSNAME', @marca=@marca,@tabla=@tabla,@campo=@campo;
        FETCH NEXT FROM elegidas INTO @tabla, @clave, @campo;
    END;
    CLOSE elegidas;
    DEALLOCATE elegidas;

    IF (SELECT COUNT(*) FROM #Cambios)<>9
       OR (SELECT COUNT(DISTINCT Tabla) FROM #Cambios)<>3
       OR EXISTS(SELECT Tabla FROM #Cambios GROUP BY Tabla HAVING COUNT(*)<>3)
       OR EXISTS(SELECT 1 FROM #Cambios WHERE DvhOriginal<>DvhPosterior OR ValorOriginal=ValorAlterado)
        THROW 51005, 'No se cumplieron las 9 alteraciones con sus DVH originales; se revierte todo.', 1;

    COMMIT TRANSACTION;
    SELECT DB_NAME() AS BaseModificada, Tabla, Clave, Campo,
        ValorOriginal, ValorAlterado, DvhOriginal, DvhPosterior
    FROM #Cambios ORDER BY Tabla, Clave;
    PRINT 'Prueba aplicada: 3 tablas, 3 registros por tabla. DVH, DVV y referencias sin recalcular.';
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
