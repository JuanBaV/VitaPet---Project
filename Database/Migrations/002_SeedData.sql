SET NOCOUNT ON;

MERGE dbo.ErrorSistema AS target
USING
(
    VALUES
        ('ER001', 'Correo ya registrado', 'Advertencia'),
        ('ER002', 'Datos incompletos', 'Advertencia'),
        ('ER003', 'Fallo de conexión', 'Crítico'),
        ('ER004', 'Error en la carga de la lista de planes', 'Grave'),
        ('ER005', 'El cliente posee un plan incompatible con la contratación solicitada', 'Advertencia'),
        ('ER006', 'Pago rechazado', 'Advertencia'),
        ('ER007', 'No fue posible registrar la contratación del plan', 'Grave'),
        ('ER008', 'No existe una cobertura asociada', 'Advertencia'),
        ('ER009', 'No existen reintegros asociados', 'Advertencia'),
        ('ER010', 'No existen mascotas registradas', 'Advertencia'),
        ('ER011', 'No existen datos asociados', 'Advertencia'),
        ('ER012', 'No existen turnos disponibles', 'Advertencia'),
        ('ER013', 'El turno ya no se encuentra disponible', 'Advertencia'),
        ('ER014', 'No existen turnos asociados', 'Advertencia'),
        ('ER015', 'No se pudo generar la salida XML', 'Grave'),
        ('ER016', 'No existen planes registrados en el sistema', 'Advertencia'),
        ('ER017', 'No existen empresas registradas', 'Advertencia'),
        ('ER018', 'Credenciales incorrectas', 'Advertencia'),
        ('ER019', 'No existen registros de bitácora', 'Advertencia'),
        ('ER020', 'No existen usuarios registrados', 'Advertencia'),
        ('ER021', 'Error al obtener los archivos de respaldo', 'Crítico'),
        ('ER022', 'No existen archivos de respaldo', 'Advertencia'),
        ('ER023', 'Inconsistencia en la verificación de integridad', 'Crítico'),
        ('ER024', 'Usuario inactivo o bloqueado', 'Advertencia'),
        ('ER025', 'Correo electrónico o respuesta incorrectos', 'Advertencia'),
        ('ER026', 'Las contraseñas no coinciden', 'Advertencia'),
        ('ER027', 'No posee autorización para restablecer la contraseña', 'Advertencia')
) AS source (codigo, descripcion, criticidad)
ON target.codigo = source.codigo
WHEN MATCHED AND
    (target.descripcion <> source.descripcion OR target.criticidad <> source.criticidad)
    THEN UPDATE SET
        descripcion = source.descripcion,
        criticidad = source.criticidad
WHEN NOT MATCHED BY TARGET
    THEN INSERT (codigo, descripcion, criticidad)
         VALUES (source.codigo, source.descripcion, source.criticidad);

IF NOT EXISTS (SELECT 1 FROM dbo.Usuario)
BEGIN
    SET IDENTITY_INSERT dbo.Usuario ON;

    INSERT INTO dbo.Usuario
        (idUsuario, correoElectronico, contrasena, nombre, rol, estado,
         intentosFallidos, bloqueado, idioma, dvh)
    VALUES
        (1, 'cliente@vitapet.local', '9281b6ae27ce6d0be049a1d17c150a23039bfbd8389f62075c743ff7b59a9b13', 'Cliente de Prueba', 'Cliente', 1, 0, 0, 'es', 'a610a172d4160c18e38c2de7b5b25118eaf65c2c855bb1a3ba9eac088acfd15f'),
        (2, 'administrador@vitapet.local', '9281b6ae27ce6d0be049a1d17c150a23039bfbd8389f62075c743ff7b59a9b13', 'Administrador de Prueba', 'Administrador', 1, 0, 0, 'es', 'ebc83568c50b04b0ef911832bf8c69e0e09ab1978d3d15e48ce92a89c0682447'),
        (3, 'webmaster@vitapet.local', '9281b6ae27ce6d0be049a1d17c150a23039bfbd8389f62075c743ff7b59a9b13', 'Webmaster de Prueba', 'Webmaster', 1, 0, 0, 'es', '1423dd2571c01c043327fd960c0f9c04d9e00a1a73a99d7e0d3b8fbeae1ee98c');

    SET IDENTITY_INSERT dbo.Usuario OFF;

    IF EXISTS (SELECT 1 FROM dbo.DVV WHERE nombreTabla = 'Usuario')
    BEGIN
        UPDATE dbo.DVV
        SET valorDVV = '9c0ccb1eed7b70664be246dbc9649f00d81b4446828833ad8f699283a69fd5a1'
        WHERE nombreTabla = 'Usuario';
    END
    ELSE
    BEGIN
        INSERT INTO dbo.DVV (nombreTabla, valorDVV)
        VALUES ('Usuario', '9c0ccb1eed7b70664be246dbc9649f00d81b4446828833ad8f699283a69fd5a1');
    END;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Bitacora) AND
   NOT EXISTS (SELECT 1 FROM dbo.DVV WHERE nombreTabla = 'Bitacora')
BEGIN
    INSERT INTO dbo.DVV (nombreTabla, valorDVV)
    VALUES ('Bitacora', 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855');
END;
