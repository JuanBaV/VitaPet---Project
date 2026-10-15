-- Align validation codes with the approved CU-007-001/CU-007-003 catalog.
-- Recovery has its own ER028/ER029/ER030 codes since migration 005.
MERGE dbo.ErrorSistema AS target
USING (VALUES
    ('ER025', 'Datos inválidos', 'Advertencia'),
    ('ER026', 'Datos incompletos', 'Advertencia'),
    ('ER027', 'La fecha desde es posterior a la fecha hasta', 'Advertencia'),
    ('ER031', 'No se pudo leer el histórico XML de bitácora', 'Grave'),
    ('ER032', 'No se pudo completar la consulta de bitácora', 'Grave')
) AS source (codigo, descripcion, criticidad)
ON target.codigo = source.codigo
WHEN MATCHED THEN UPDATE SET descripcion = source.descripcion, criticidad = source.criticidad
WHEN NOT MATCHED THEN INSERT (codigo, descripcion, criticidad)
VALUES (source.codigo, source.descripcion, source.criticidad);
