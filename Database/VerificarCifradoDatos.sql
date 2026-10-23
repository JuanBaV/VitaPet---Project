-- Punto 20. Consulta de solo lectura; no necesita el archivo de clave.
-- Comprueba el formato externo. La aplicación verifica además HMAC, descifrado y DVH/DVV.
SET NOCOUNT ON;
SELECT DB_NAME() AS BaseComprobada;
SELECT name AS ClaveSQL,algorithm_desc AS Algoritmo,key_length AS Bits
FROM sys.symmetric_keys WHERE name='VitaPet_DatosV1';
SELECT migrationId,appliedUtc FROM dbo.__SchemaMigrations
WHERE migrationId='034_CifradoDatosPersonales.sql';

SELECT N'Usuario' AS Tabla,N'correoElectronico' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([correoElectronico]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [correoElectronico] IS NOT NULL AND
        (LEFT([correoElectronico],4)<>'VP1:' OR LEN([correoElectronico])<132 OR SUBSTRING([correoElectronico],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Usuario]
UNION ALL
SELECT N'Usuario' AS Tabla,N'nombre' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([nombre]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [nombre] IS NOT NULL AND
        (LEFT([nombre],4)<>'VP1:' OR LEN([nombre])<132 OR SUBSTRING([nombre],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Usuario]
UNION ALL
SELECT N'Cliente' AS Tabla,N'nombre' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([nombre]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [nombre] IS NOT NULL AND
        (LEFT([nombre],4)<>'VP1:' OR LEN([nombre])<132 OR SUBSTRING([nombre],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Cliente]
UNION ALL
SELECT N'Cliente' AS Tabla,N'apellido' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([apellido]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [apellido] IS NOT NULL AND
        (LEFT([apellido],4)<>'VP1:' OR LEN([apellido])<132 OR SUBSTRING([apellido],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Cliente]
UNION ALL
SELECT N'Cliente' AS Tabla,N'telefono' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([telefono]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [telefono] IS NOT NULL AND
        (LEFT([telefono],4)<>'VP1:' OR LEN([telefono])<132 OR SUBSTRING([telefono],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Cliente]
UNION ALL
SELECT N'Empresa' AS Tabla,N'razonSocial' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([razonSocial]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [razonSocial] IS NOT NULL AND
        (LEFT([razonSocial],4)<>'VP1:' OR LEN([razonSocial])<132 OR SUBSTRING([razonSocial],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Empresa]
UNION ALL
SELECT N'Empresa' AS Tabla,N'cuit' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([cuit]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [cuit] IS NOT NULL AND
        (LEFT([cuit],4)<>'VP1:' OR LEN([cuit])<132 OR SUBSTRING([cuit],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Empresa]
UNION ALL
SELECT N'Empresa' AS Tabla,N'direccion' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([direccion]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [direccion] IS NOT NULL AND
        (LEFT([direccion],4)<>'VP1:' OR LEN([direccion])<132 OR SUBSTRING([direccion],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Empresa]
UNION ALL
SELECT N'Empresa' AS Tabla,N'telefono' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([telefono]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [telefono] IS NOT NULL AND
        (LEFT([telefono],4)<>'VP1:' OR LEN([telefono])<132 OR SUBSTRING([telefono],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Empresa]
UNION ALL
SELECT N'DetallePago' AS Tabla,N'titular' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([titular]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [titular] IS NOT NULL AND
        (LEFT([titular],4)<>'VP1:' OR LEN([titular])<132 OR SUBSTRING([titular],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[DetallePago]
UNION ALL
SELECT N'DetallePago' AS Tabla,N'correoFactura' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([correoFactura]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [correoFactura] IS NOT NULL AND
        (LEFT([correoFactura],4)<>'VP1:' OR LEN([correoFactura])<132 OR SUBSTRING([correoFactura],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[DetallePago]
UNION ALL
SELECT N'Factura' AS Tabla,N'clienteNombre' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([clienteNombre]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [clienteNombre] IS NOT NULL AND
        (LEFT([clienteNombre],4)<>'VP1:' OR LEN([clienteNombre])<132 OR SUBSTRING([clienteNombre],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Factura]
UNION ALL
SELECT N'Factura' AS Tabla,N'clienteCorreo' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([clienteCorreo]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [clienteCorreo] IS NOT NULL AND
        (LEFT([clienteCorreo],4)<>'VP1:' OR LEN([clienteCorreo])<132 OR SUBSTRING([clienteCorreo],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Factura]
UNION ALL
SELECT N'Factura' AS Tabla,N'titular' AS Campo,COUNT_BIG(*) AS Filas,
    COUNT_BIG([titular]) AS ValoresNoNulos,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN [titular] IS NOT NULL AND
        (LEFT([titular],4)<>'VP1:' OR LEN([titular])<132 OR SUBSTRING([titular],5,2044) COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9A-F]%')
        THEN 1 ELSE 0 END)),0) AS ValoresSinFormatoCifrado
FROM dbo.[Factura];

SELECT COUNT_BIG(*) AS Usuarios,
    COALESCE(SUM(CONVERT(bigint,CASE WHEN LEN(contrasena)<>64 OR contrasena COLLATE Latin1_General_100_BIN2 LIKE '%[^0-9a-fA-F]%' THEN 1 ELSE 0 END)),0) AS ContrasenasSinFormatoHash
FROM dbo.Usuario;

