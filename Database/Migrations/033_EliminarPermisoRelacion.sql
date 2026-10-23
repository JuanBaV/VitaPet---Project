-- Conserva cualquier composición de un respaldo anterior en la relación única RolPermiso.
INSERT dbo.Rol(idRol,dvh)
SELECT p.idComponente,'' FROM dbo.PermisoComponente p
WHERE p.tipo='Perfil' AND NOT EXISTS(SELECT 1 FROM dbo.Rol r WHERE r.idRol=p.idComponente);
INSERT dbo.RolPermiso(idRol,idComponente,dvh)
SELECT p.idPadre,p.idHijo,'' FROM dbo.PermisoRelacion p
WHERE NOT EXISTS(SELECT 1 FROM dbo.RolPermiso r WHERE r.idRol=p.idPadre AND r.idComponente=p.idHijo);
DROP TABLE dbo.PermisoRelacion;
DELETE dbo.DVV WHERE nombreTabla='PermisoRelacion';
GO

CREATE OR ALTER FUNCTION dbo.fn_RolPermite(@idRol INT,@pantallas VARCHAR(1000))
RETURNS BIT
AS
BEGIN
    DECLARE @permitido BIT=0;
    WITH componentes AS (
        SELECT c.idComponente,c.pantalla FROM dbo.Rol r JOIN dbo.PermisoComponente c ON c.idComponente=r.idRol WHERE r.idRol=@idRol
        UNION ALL
        SELECT hijo.idComponente,hijo.pantalla FROM componentes padre
        JOIN dbo.RolPermiso r ON r.idRol=padre.idComponente
        JOIN dbo.PermisoComponente hijo ON hijo.idComponente=r.idComponente
    )
    SELECT TOP(1) @permitido=1 FROM componentes
    WHERE CHARINDEX(','+pantalla+',',','+@pantallas+',')>0;
    RETURN @permitido;
END;
GO
