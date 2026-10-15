SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.Usuario', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Usuario
    (
        idUsuario INT IDENTITY(1,1) NOT NULL,
        correoElectronico VARCHAR(100) NOT NULL,
        contrasena VARCHAR(255) NOT NULL,
        nombre VARCHAR(100) NOT NULL,
        rol VARCHAR(30) NOT NULL,
        estado BIT NOT NULL CONSTRAINT DF_Usuario_estado DEFAULT (1),
        intentosFallidos TINYINT NOT NULL CONSTRAINT DF_Usuario_intentosFallidos DEFAULT (0),
        bloqueado BIT NOT NULL CONSTRAINT DF_Usuario_bloqueado DEFAULT (0),
        idioma VARCHAR(5) NOT NULL CONSTRAINT DF_Usuario_idioma DEFAULT ('es'),
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Usuario PRIMARY KEY (idUsuario),
        CONSTRAINT UQ_Usuario_correoElectronico UNIQUE (correoElectronico),
        CONSTRAINT CK_Usuario_rol CHECK (rol IN ('Cliente', 'Administrador', 'Webmaster')),
        CONSTRAINT CK_Usuario_intentosFallidos CHECK (intentosFallidos BETWEEN 0 AND 3),
        CONSTRAINT CK_Usuario_idioma CHECK (idioma IN ('es', 'en'))
    );
END;

IF OBJECT_ID(N'dbo.Empresa', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Empresa
    (
        idEmpresa INT IDENTITY(1,1) NOT NULL,
        razonSocial VARCHAR(100) NOT NULL,
        cuit VARCHAR(20) NOT NULL,
        direccion VARCHAR(150) NOT NULL,
        telefono VARCHAR(30) NOT NULL,
        estado BIT NOT NULL CONSTRAINT DF_Empresa_estado DEFAULT (1),
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Empresa PRIMARY KEY (idEmpresa),
        CONSTRAINT UQ_Empresa_cuit UNIQUE (cuit)
    );
END;

IF OBJECT_ID(N'dbo.Cliente', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cliente
    (
        idCliente INT IDENTITY(1,1) NOT NULL,
        idUsuario INT NOT NULL,
        idEmpresa INT NULL,
        nombre VARCHAR(60) NOT NULL,
        apellido VARCHAR(60) NOT NULL,
        telefono VARCHAR(30) NOT NULL,
        tipoCliente VARCHAR(20) NOT NULL,
        estado BIT NOT NULL CONSTRAINT DF_Cliente_estado DEFAULT (1),
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Cliente PRIMARY KEY (idCliente),
        CONSTRAINT UQ_Cliente_idUsuario UNIQUE (idUsuario),
        CONSTRAINT FK_Cliente_Usuario FOREIGN KEY (idUsuario) REFERENCES dbo.Usuario(idUsuario),
        CONSTRAINT FK_Cliente_Empresa FOREIGN KEY (idEmpresa) REFERENCES dbo.Empresa(idEmpresa)
    );
END;

IF OBJECT_ID(N'dbo.Plan', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.[Plan]
    (
        idPlan INT IDENTITY(1,1) NOT NULL,
        nombre VARCHAR(80) NOT NULL,
        descripcion VARCHAR(255) NOT NULL,
        precio DECIMAL(10,2) NOT NULL,
        estado BIT NOT NULL CONSTRAINT DF_Plan_estado DEFAULT (1),
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Plan PRIMARY KEY (idPlan),
        CONSTRAINT UQ_Plan_nombre UNIQUE (nombre),
        CONSTRAINT CK_Plan_precio CHECK (precio >= 0)
    );
END;

IF OBJECT_ID(N'dbo.Cobertura', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Cobertura
    (
        idCobertura INT IDENTITY(1,1) NOT NULL,
        idPlan INT NOT NULL,
        prestacion VARCHAR(100) NOT NULL,
        detalle VARCHAR(255) NOT NULL,
        estado BIT NOT NULL CONSTRAINT DF_Cobertura_estado DEFAULT (1),
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Cobertura PRIMARY KEY (idCobertura),
        CONSTRAINT FK_Cobertura_Plan FOREIGN KEY (idPlan) REFERENCES dbo.[Plan](idPlan)
    );
END;

IF OBJECT_ID(N'dbo.Suscripcion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suscripcion
    (
        idSuscripcion INT IDENTITY(1,1) NOT NULL,
        idCliente INT NOT NULL,
        idPlan INT NOT NULL,
        fechaAlta DATE NOT NULL,
        fechaVencimiento DATE NOT NULL,
        estado VARCHAR(30) NOT NULL,
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Suscripcion PRIMARY KEY (idSuscripcion),
        CONSTRAINT FK_Suscripcion_Cliente FOREIGN KEY (idCliente) REFERENCES dbo.Cliente(idCliente),
        CONSTRAINT FK_Suscripcion_Plan FOREIGN KEY (idPlan) REFERENCES dbo.[Plan](idPlan),
        CONSTRAINT CK_Suscripcion_fechas CHECK (fechaVencimiento >= fechaAlta)
    );
END;

IF OBJECT_ID(N'dbo.Pago', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pago
    (
        idPago INT IDENTITY(1,1) NOT NULL,
        idSuscripcion INT NOT NULL,
        importe DECIMAL(10,2) NOT NULL,
        medioPago VARCHAR(40) NOT NULL,
        fechaPago DATE NOT NULL,
        estado VARCHAR(30) NOT NULL,
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Pago PRIMARY KEY (idPago),
        CONSTRAINT FK_Pago_Suscripcion FOREIGN KEY (idSuscripcion) REFERENCES dbo.Suscripcion(idSuscripcion),
        CONSTRAINT CK_Pago_importe CHECK (importe >= 0)
    );
END;

IF OBJECT_ID(N'dbo.Mascota', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Mascota
    (
        idMascota INT IDENTITY(1,1) NOT NULL,
        idCliente INT NOT NULL,
        nombre VARCHAR(60) NOT NULL,
        especie VARCHAR(40) NOT NULL,
        raza VARCHAR(60) NOT NULL,
        fechaNacimiento DATE NOT NULL,
        estado BIT NOT NULL CONSTRAINT DF_Mascota_estado DEFAULT (1),
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Mascota PRIMARY KEY (idMascota),
        CONSTRAINT FK_Mascota_Cliente FOREIGN KEY (idCliente) REFERENCES dbo.Cliente(idCliente),
        CONSTRAINT CK_Mascota_fechaNacimiento CHECK (fechaNacimiento <= CONVERT(DATE, GETDATE()))
    );
END;

IF OBJECT_ID(N'dbo.Turno', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Turno
    (
        idTurno INT IDENTITY(1,1) NOT NULL,
        idMascota INT NOT NULL,
        fecha DATE NOT NULL,
        horario VARCHAR(20) NOT NULL,
        prestacion VARCHAR(100) NOT NULL,
        zona VARCHAR(60) NOT NULL,
        estado VARCHAR(30) NOT NULL,
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Turno PRIMARY KEY (idTurno),
        CONSTRAINT FK_Turno_Mascota FOREIGN KEY (idMascota) REFERENCES dbo.Mascota(idMascota)
    );
END;

IF OBJECT_ID(N'dbo.HistorialClinico', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistorialClinico
    (
        idHistorial INT IDENTITY(1,1) NOT NULL,
        idMascota INT NOT NULL,
        fechaRegistro DATETIME NOT NULL,
        diagnostico VARCHAR(255) NOT NULL,
        tratamiento VARCHAR(255) NOT NULL,
        observaciones VARCHAR(255) NULL,
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_HistorialClinico PRIMARY KEY (idHistorial),
        CONSTRAINT FK_HistorialClinico_Mascota FOREIGN KEY (idMascota) REFERENCES dbo.Mascota(idMascota)
    );
END;

IF OBJECT_ID(N'dbo.Reintegro', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Reintegro
    (
        idReintegro INT IDENTITY(1,1) NOT NULL,
        idCliente INT NOT NULL,
        idMascota INT NOT NULL,
        fechaSolicitud DATETIME NOT NULL,
        importe DECIMAL(10,2) NOT NULL,
        estado VARCHAR(30) NOT NULL,
        detalle VARCHAR(255) NULL,
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Reintegro PRIMARY KEY (idReintegro),
        CONSTRAINT FK_Reintegro_Cliente FOREIGN KEY (idCliente) REFERENCES dbo.Cliente(idCliente),
        CONSTRAINT FK_Reintegro_Mascota FOREIGN KEY (idMascota) REFERENCES dbo.Mascota(idMascota),
        CONSTRAINT CK_Reintegro_importe CHECK (importe >= 0)
    );
END;

IF OBJECT_ID(N'dbo.Bitacora', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Bitacora
    (
        idBitacora INT IDENTITY(1,1) NOT NULL,
        idUsuario INT NULL,
        fecha DATETIME NOT NULL CONSTRAINT DF_Bitacora_fecha DEFAULT (GETDATE()),
        accion VARCHAR(100) NOT NULL,
        modulo VARCHAR(50) NOT NULL,
        criticidad VARCHAR(20) NOT NULL,
        dvh VARCHAR(64) NOT NULL,
        CONSTRAINT PK_Bitacora PRIMARY KEY (idBitacora),
        CONSTRAINT FK_Bitacora_Usuario FOREIGN KEY (idUsuario) REFERENCES dbo.Usuario(idUsuario),
        CONSTRAINT CK_Bitacora_criticidad CHECK (criticidad IN ('Informativa', 'Advertencia', 'Grave', 'Crítica'))
    );
END;

IF OBJECT_ID(N'dbo.DVV', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DVV
    (
        idDVV INT IDENTITY(1,1) NOT NULL,
        nombreTabla VARCHAR(60) NOT NULL,
        valorDVV VARCHAR(255) NOT NULL,
        CONSTRAINT PK_DVV PRIMARY KEY (idDVV),
        CONSTRAINT UQ_DVV_nombreTabla UNIQUE (nombreTabla)
    );
END;

IF OBJECT_ID(N'dbo.ErrorSistema', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ErrorSistema
    (
        codigo VARCHAR(10) NOT NULL,
        descripcion VARCHAR(255) NOT NULL,
        criticidad VARCHAR(20) NOT NULL,
        CONSTRAINT PK_ErrorSistema PRIMARY KEY (codigo),
        CONSTRAINT CK_ErrorSistema_criticidad CHECK (criticidad IN ('Advertencia', 'Grave', 'Crítico'))
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Cliente') AND name = N'IX_Cliente_idEmpresa')
    CREATE INDEX IX_Cliente_idEmpresa ON dbo.Cliente(idEmpresa);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Cobertura') AND name = N'IX_Cobertura_idPlan')
    CREATE INDEX IX_Cobertura_idPlan ON dbo.Cobertura(idPlan);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Suscripcion') AND name = N'IX_Suscripcion_idCliente')
    CREATE INDEX IX_Suscripcion_idCliente ON dbo.Suscripcion(idCliente);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Suscripcion') AND name = N'IX_Suscripcion_idPlan')
    CREATE INDEX IX_Suscripcion_idPlan ON dbo.Suscripcion(idPlan);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Pago') AND name = N'IX_Pago_idSuscripcion')
    CREATE INDEX IX_Pago_idSuscripcion ON dbo.Pago(idSuscripcion);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Mascota') AND name = N'IX_Mascota_idCliente')
    CREATE INDEX IX_Mascota_idCliente ON dbo.Mascota(idCliente);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Turno') AND name = N'IX_Turno_idMascota')
    CREATE INDEX IX_Turno_idMascota ON dbo.Turno(idMascota);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.HistorialClinico') AND name = N'IX_HistorialClinico_idMascota_fechaRegistro')
    CREATE INDEX IX_HistorialClinico_idMascota_fechaRegistro ON dbo.HistorialClinico(idMascota, fechaRegistro);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Reintegro') AND name = N'IX_Reintegro_idCliente')
    CREATE INDEX IX_Reintegro_idCliente ON dbo.Reintegro(idCliente);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Reintegro') AND name = N'IX_Reintegro_idMascota')
    CREATE INDEX IX_Reintegro_idMascota ON dbo.Reintegro(idMascota);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Bitacora') AND name = N'IX_Bitacora_fecha_idBitacora')
    CREATE INDEX IX_Bitacora_fecha_idBitacora ON dbo.Bitacora(fecha DESC, idBitacora DESC);
