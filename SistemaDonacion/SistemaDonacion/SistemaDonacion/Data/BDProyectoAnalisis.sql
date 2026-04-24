-- Crear base de datos si no existe
CREATE DATABASE SistemaDonacionDb

USE SistemaDonacionDb

-- Crear tabla Hospitales
IF OBJECT_ID(N'dbo.Hospitales', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Hospitales (
                              Id INT PRIMARY KEY IDENTITY(1,1),
                              Nombre NVARCHAR(256) NOT NULL UNIQUE,
                              Ciudad NVARCHAR(256) NOT NULL,
                              Pais NVARCHAR(100) NOT NULL,
                              Telefono NVARCHAR(20),
                              Email NVARCHAR(256),
                              Estado BIT NOT NULL DEFAULT 1,
                              FechaRegistro DATETIME NOT NULL DEFAULT GETDATE()
)

CREATE INDEX IX_Hospitales_Nombre ON dbo.Hospitales (Nombre)
CREATE INDEX IX_Hospitales_Estado ON dbo.Hospitales (Estado)
END
GO

PRINT 'Tabla Hospitales creada/verificada exitosamente.'

-- Insertar hospitales de prueba
IF NOT EXISTS (SELECT 1 FROM dbo.Hospitales WHERE Nombre = 'Hospital Central')
BEGIN
INSERT INTO dbo.Hospitales (Nombre, Ciudad, Pais, Telefono, Email, Estado)
VALUES 
  ('Hospital Central', 'Ciudad de Guatemala', 'Guatemala', '5557-1234', 'centralguate@hospital.com', 1),
  ('Hospital Nicolasa Cruz', 'Ciudad de Jalapa', 'Jalapa', '5551-5678', 'nicolasa@hospital.com', 1);
PRINT 'Hospitales de prueba insertados'
END
ELSE
BEGIN
    PRINT 'Hospitales ya existen'
END
GO

-- Crear tabla Usuarios
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Usuarios (
                              Id INT PRIMARY KEY IDENTITY(1,1),
                              Nombre NVARCHAR(256) NOT NULL UNIQUE,
                              Contrasenia NVARCHAR(MAX) NOT NULL,
                              Estado BIT NOT NULL DEFAULT 1,
                              Rol NVARCHAR(50) NOT NULL DEFAULT 'Medico'
)

CREATE INDEX IX_Usuarios_Nombre ON dbo.Usuarios (Nombre)
END
GO

PRINT 'Tabla Usuarios creada/verificada exitosamente.'

-- Hash generado con PBKDF2: $PBKDF2$10000$[salt]$[hash]
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Nombre = 'admin')
BEGIN
INSERT INTO dbo.Usuarios (Nombre, Contrasenia, Estado, Rol)
VALUES (
           'admin',
           '$PBKDF2$10000$8T8EIKKp3WxYQJp2KeJKvA==$vIZ6M3QaY7T/oPu+QsJ0TuXYzOvs7G8YkR+2qZvFhMI=',
           1,
           'Administrador'
       );
PRINT 'Usuario admin creado: usuario=admin, contraseña=Admin123!'
END
ELSE
BEGIN
    PRINT 'Usuario admin ya existe.'
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Nombre = 'medico1')
BEGIN
INSERT INTO dbo.Usuarios (Nombre, Contrasenia, Estado, Rol)
VALUES (
           'medico1',
           '$PBKDF2$10000$PBPYvv07oE+ZTjggclVYmA==$nzOtI1jl67AjxOGYaRjweYFxLX6slRPP1zBRc60kw8A=',
           1,
           'Medico'
       );
PRINT 'Usuario médico creado: usuario=medico1, contraseña=Medico123!'
END
ELSE
BEGIN
    PRINT 'Usuario medico1 ya existe.'
END
GO

-- Crear tabla Donantes
IF OBJECT_ID(N'dbo.Donantes', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Donantes (
                              Id INT PRIMARY KEY IDENTITY(1,1),
                              Nombre NVARCHAR(256) NOT NULL,
                              TipoSanguineo NVARCHAR(10) NOT NULL,
                              Edad INT NOT NULL,
                              FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
                              Estado NVARCHAR(50) NOT NULL DEFAULT 'Disponible',
                              HospitalId INT NOT NULL,
                              Observaciones NVARCHAR(MAX),
                              FechaActualizacion DATETIME NOT NULL DEFAULT GETDATE(),
                              FOREIGN KEY (HospitalId) REFERENCES dbo.Hospitales(Id)
)

CREATE INDEX IX_Donantes_Estado ON dbo.Donantes (Estado)
CREATE INDEX IX_Donantes_HospitalId ON dbo.Donantes (HospitalId)
CREATE INDEX IX_Donantes_TipoSanguineo ON dbo.Donantes (TipoSanguineo)
END
GO

PRINT 'Tabla Donantes creada/verificada exitosamente.'

-- Crear tabla Organos
IF OBJECT_ID(N'dbo.Organos', N'U') IS NULL
BEGIN
CREATE TABLE dbo.Organos (
                             Id INT PRIMARY KEY IDENTITY(1,1),
                             DonanteId INT NOT NULL,
                             TipoOrgano NVARCHAR(100) NOT NULL,
                             Estado NVARCHAR(50) NOT NULL DEFAULT 'Disponible',
                             FechaDisponibilidad DATETIME NOT NULL DEFAULT GETDATE(),
                             Compatibilidad NVARCHAR(MAX),
                             FechaActualizacion DATETIME NOT NULL DEFAULT GETDATE(),
                             FOREIGN KEY (DonanteId) REFERENCES dbo.Donantes(Id) ON DELETE CASCADE
)

CREATE INDEX IX_Organos_DonanteId ON dbo.Organos (DonanteId)
CREATE INDEX IX_Organos_Estado ON dbo.Organos (Estado)
CREATE INDEX IX_Organos_TipoOrgano ON dbo.Organos (TipoOrgano)
END
GO

PRINT 'Tabla Organos creada/verificada exitosamente.'

-- Crear tabla BitacoraAcciones (auditoría)
IF OBJECT_ID(N'dbo.BitacoraAcciones', N'U') IS NULL
BEGIN
CREATE TABLE dbo.BitacoraAcciones (
                                      Id INT PRIMARY KEY IDENTITY(1,1),
                                      UsuarioId INT NOT NULL,
                                      Accion NVARCHAR(256) NOT NULL,
                                      Tabla NVARCHAR(100) NOT NULL,
                                      RegistroId INT NOT NULL,
                                      DatosAnteriores NVARCHAR(MAX),
                                      DatosNuevos NVARCHAR(MAX),
                                      FechaAccion DATETIME NOT NULL DEFAULT GETDATE(),
                                      Detalles NVARCHAR(MAX),
                                      FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id)
)

CREATE INDEX IX_BitacoraAcciones_UsuarioId ON dbo.BitacoraAcciones (UsuarioId)
CREATE INDEX IX_BitacoraAcciones_Tabla ON dbo.BitacoraAcciones (Tabla)
CREATE INDEX IX_BitacoraAcciones_FechaAccion ON dbo.BitacoraAcciones (FechaAccion)
END
GO

PRINT 'Tabla BitacoraAcciones creada/verificada exitosamente.'

IF OBJECT_ID(N'dbo.Pacientes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Pacientes (
        Id INT PRIMARY KEY IDENTITY(1,1),
        Nombre NVARCHAR(256) NOT NULL,
        TipoSanguineo NVARCHAR(10) NOT NULL,
        OrganoRequerido NVARCHAR(100) NOT NULL,
        NivelUrgencia NVARCHAR(20) NOT NULL,
        HospitalId INT NOT NULL,
        Estado NVARCHAR(50) NOT NULL DEFAULT 'Activo',
        Observaciones NVARCHAR(MAX),
        FechaRegistro DATETIME NOT NULL DEFAULT GETDATE(),
        FechaActualizacion DATETIME NOT NULL DEFAULT GETDATE(),
        FOREIGN KEY (HospitalId) REFERENCES dbo.Hospitales(Id)
    )

    CREATE INDEX IX_Pacientes_Estado ON dbo.Pacientes (Estado)
    CREATE INDEX IX_Pacientes_TipoSanguineo ON dbo.Pacientes (TipoSanguineo)
    CREATE INDEX IX_Pacientes_NivelUrgencia ON dbo.Pacientes (NivelUrgencia)
END
GO
PRINT 'Tabla Pacientes creada/verificada.'

-- Verificar que todas las tablas fueron creadas
SELECT 'Tablas creadas:' AS Estado
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = 'dbo' ORDER BY TABLE_NAME

select * from Usuarios



-- Agregar columna HospitalId a Usuarios (nullable para no romper registros existentes)
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Usuarios' AND COLUMN_NAME = 'HospitalId'
)
BEGIN
    ALTER TABLE dbo.Usuarios
    ADD HospitalId INT NULL
        CONSTRAINT FK_Usuarios_Hospitales
        FOREIGN KEY REFERENCES dbo.Hospitales(Id);

    CREATE INDEX IX_Usuarios_HospitalId ON dbo.Usuarios (HospitalId);

    PRINT 'Columna HospitalId agregada a Usuarios.'
END
ELSE
BEGIN
    PRINT 'Columna HospitalId ya existe en Usuarios.'
END
GO

-- Asignar hospitales a los usuarios existentes
--    admin sin hospital (Administrador gestiona todos)
--    medico1 Hospital Central (Id = 1)
UPDATE dbo.Usuarios
SET HospitalId = NULL
WHERE Nombre = 'admin';

UPDATE dbo.Usuarios
SET HospitalId = 1
WHERE Nombre = 'medico1';

PRINT 'HospitalId asignado a usuarios existentes.'
GO

-- Crear usuario médico de prueba para el segundo hospital
IF NOT EXISTS (SELECT 1 FROM dbo.Usuarios WHERE Nombre = 'medico2')
BEGIN
    -- Hash PBKDF2 para "Medico123!" (reutilizando el mismo hash de medico1 para pruebas)
    INSERT INTO dbo.Usuarios (Nombre, Contrasenia, Estado, Rol, HospitalId)
    VALUES (
        'medico2',
        '$PBKDF2$10000$PBPYvv07oE+ZTjggclVYmA==$nzOtI1jl67AjxOGYaRjweYFxLX6slRPP1zBRc60kw8A=',
        1,
        'Medico',
        2  -- Hospital Nicolasa Cruz
    );
    PRINT 'Usuario medico2 creado para Hospital Nicolasa Cruz (Id=2).'
END
GO

-- Insertar pacientes de prueba por hospital para validar el filtrado
IF NOT EXISTS (SELECT 1 FROM dbo.Pacientes WHERE Nombre = 'Paciente Hospital Central 1')
BEGIN
    INSERT INTO dbo.Pacientes (Nombre, TipoSanguineo, OrganoRequerido, NivelUrgencia, HospitalId, Estado)
    VALUES
        ('Carlos Andrés Pérez', 'O+',  'Riñón',    'Alta',  1, 'Activo'),
        ('Cristina Nicoll Chaj', 'A-',  'Hígado',   'Media', 1, 'Activo'),
        ('Juan Pablo Méndez',    'B+',  'Corazón',  'Alta',  2, 'Activo'),
        ('Ana Lucía Ramírez',    'AB+', 'Pulmón',   'Baja',  2, 'Activo');
    PRINT 'Pacientes de prueba insertados por hospital.'
END
GO

--  Verificación final
SELECT
    u.Nombre AS Usuario,
    u.Rol,
    h.Nombre AS Hospital,
    u.HospitalId
FROM dbo.Usuarios u
LEFT JOIN dbo.Hospitales h ON u.HospitalId = h.Id
ORDER BY u.Id;

SELECT
    p.Nombre AS Paciente,
    p.HospitalId,
    h.Nombre AS Hospital
FROM dbo.Pacientes p
JOIN dbo.Hospitales h ON p.HospitalId = h.Id
ORDER BY p.HospitalId, p.Id;



