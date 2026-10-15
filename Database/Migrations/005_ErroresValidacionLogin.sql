MERGE dbo.ErrorSistema AS target
USING
(
    VALUES
        ('ER025', 'Datos incompletos', 'Advertencia'),
        ('ER026', 'Datos inválidos', 'Advertencia'),
        ('ER028', 'Correo electrónico o respuesta incorrectos', 'Advertencia'),
        ('ER029', 'Las contraseñas no coinciden', 'Advertencia'),
        ('ER030', 'No posee autorización para restablecer la contraseña', 'Advertencia')
) AS source (codigo, descripcion, criticidad)
ON target.codigo = source.codigo
WHEN MATCHED AND
    (target.descripcion <> source.descripcion OR
     target.criticidad <> source.criticidad)
    THEN UPDATE SET
        descripcion = source.descripcion,
        criticidad = source.criticidad
WHEN NOT MATCHED BY TARGET
    THEN INSERT (codigo, descripcion, criticidad)
         VALUES (source.codigo, source.descripcion, source.criticidad);
