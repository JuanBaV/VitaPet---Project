IF NOT EXISTS (SELECT 1 FROM dbo.ErrorSistema WHERE codigo = 'ER034')
    INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
    VALUES ('ER034', 'No se pudo completar el cierre de sesión', 'Grave');
