-- Los perfiles iniciales se crean desde BLL después de verificar la integridad.
CREATE TABLE dbo.PermisoComponente (
    idComponente INT IDENTITY PRIMARY KEY,
    nombre VARCHAR(100) NOT NULL UNIQUE,
    tipo VARCHAR(10) NOT NULL CHECK(tipo IN ('Perfil','Pantalla')),
    pantalla VARCHAR(80) NULL,
    sistema BIT NOT NULL,
    dvh VARCHAR(64) NOT NULL
);
CREATE UNIQUE INDEX UX_Permiso_Pantalla ON dbo.PermisoComponente(pantalla) WHERE pantalla IS NOT NULL;
CREATE TABLE dbo.PermisoRelacion (
    idRelacion INT IDENTITY PRIMARY KEY,
    idPadre INT NOT NULL REFERENCES dbo.PermisoComponente(idComponente),
    idHijo INT NOT NULL REFERENCES dbo.PermisoComponente(idComponente),
    dvh VARCHAR(64) NOT NULL,
    UNIQUE(idPadre,idHijo), CHECK(idPadre<>idHijo)
);
ALTER TABLE dbo.Usuario DROP CONSTRAINT CK_Usuario_rol;
INSERT dbo.DVV(nombreTabla,valorDVV) VALUES
('PermisoComponente','e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855'),
('PermisoRelacion','e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855');
GO
