-- Punto 19 del checklist. Ejecutar sobre la base VitaPet en SQL Server.
-- Solo consulta: no recalcula verificadores ni modifica datos.
-- Las cuatro tablas de control se validan mediante referencias, formato y
-- checksums de migraciones; no llevan un DVH/DVV propio autorreferencial.
-- Esta consulta comprueba cobertura y formato. Para recalcular y comparar
-- las huellas, usar Base de datos > Verificar integridad en la aplicación.
SET NOCOUNT ON;

SELECT DB_NAME() AS BaseComprobada;

SELECT s.name AS Esquema, t.name AS Tabla,
    CASE WHEN t.name IN ('DVV', 'ReferenciaIntegridad', '__FormatoIntegridad', '__SchemaMigrations')
        THEN N'Control técnico' ELSE N'Datos' END AS Tipo,
    CASE WHEN c.column_id IS NULL THEN 0 ELSE 1 END AS TieneDVH,
    CASE WHEN d.idDVV IS NULL THEN 0 ELSE 1 END AS TieneDVV,
    LEN(d.valorDVV) AS LongitudDVV
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
LEFT JOIN sys.columns c ON c.object_id = t.object_id AND c.name = 'dvh'
LEFT JOIN dbo.DVV d ON d.nombreTabla = t.name AND s.name = 'dbo'
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name;

-- Un hash válido tiene 64 caracteres hexadecimales. Un resultado de cero
-- no prueba su correspondencia con los datos: esa comparación la hace la BLL.
DECLARE @sql nvarchar(max) = N'';
SELECT @sql = @sql + CASE WHEN @sql = N'' THEN N'' ELSE N' UNION ALL ' END
    + N'SELECT N''' + REPLACE(s.name + N'.' + t.name, N'''', N'''''')
    + N''' AS Tabla, COUNT_BIG(*) AS Filas, '
    + N'COALESCE(SUM(CONVERT(bigint, CASE WHEN dvh IS NULL OR DATALENGTH(dvh) <> 64 '
    + N'OR dvh COLLATE Latin1_General_100_BIN2 LIKE ''%[^0-9a-fA-F]%'' '
    + N'THEN 1 ELSE 0 END)), 0) AS DVHConFormatoInvalido FROM '
    + QUOTENAME(s.name) + N'.' + QUOTENAME(t.name)
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id AND c.name = 'dvh'
WHERE t.is_ms_shipped = 0;

IF @sql <> N'' EXEC sys.sp_executesql @sql;
