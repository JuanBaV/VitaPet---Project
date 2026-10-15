IF NOT EXISTS (SELECT 1 FROM dbo.ErrorSistema WHERE codigo = 'ER033')
    INSERT INTO dbo.ErrorSistema (codigo, descripcion, criticidad)
    VALUES ('ER033', 'No se pudo registrar la consulta en bitácora', 'Grave');
