USE [VitaPet]
GO

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ErrorSistema WHERE codigo = 'ER025')
BEGIN
    INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
    VALUES ('ER025', 'Correo electrónico o respuesta incorrectos', 'Advertencia');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ErrorSistema WHERE codigo = 'ER026')
BEGIN
    INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
    VALUES ('ER026', 'Las contraseñas no coinciden', 'Advertencia');
END;
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ErrorSistema WHERE codigo = 'ER027')
BEGIN
    INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
    VALUES ('ER027', 'No posee autorización para restablecer la contraseña', 'Advertencia');
END;
GO
