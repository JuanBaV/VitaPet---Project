-- CU-001-001. ER049 se incorpora desde ErrorSistemaBLL después de verificar
-- la integridad; una migración SQL no debe insertar filas sin sus referencias.
CREATE OR ALTER PROCEDURE dbo.sp_Usuario_RegistrarCliente
    @nombre VARCHAR(100), @correo VARCHAR(100), @contrasena VARCHAR(255)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT = 0 THROW 50049, 'Se requiere una transaccion de integridad.', 1;
    IF EXISTS (SELECT 1 FROM dbo.Usuario WITH (UPDLOCK, HOLDLOCK) WHERE correoElectronico=@correo)
        THROW 50001, 'Correo ya registrado.', 1;
    INSERT dbo.Usuario(nombre, correoElectronico, contrasena, rol, estado,
        intentosFallidos, bloqueado, idioma, dvh)
    OUTPUT INSERTED.idUsuario
    VALUES(@nombre, @correo, @contrasena, 'Cliente', 1, 0, 0, 'es', '');
END;
GO
CREATE OR ALTER PROCEDURE dbo.sp_Cliente_Registrar
    @idUsuario INT, @nombre VARCHAR(60), @apellido VARCHAR(60), @telefono VARCHAR(30)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF @@TRANCOUNT = 0 THROW 50049, 'Se requiere una transaccion de integridad.', 1;
    INSERT dbo.Cliente(idUsuario, idEmpresa, nombre, apellido, telefono, tipoCliente, estado, dvh)
    OUTPUT INSERTED.idCliente
    VALUES(@idUsuario, NULL, @nombre, @apellido, @telefono, 'Individual', 1, '');
END;
