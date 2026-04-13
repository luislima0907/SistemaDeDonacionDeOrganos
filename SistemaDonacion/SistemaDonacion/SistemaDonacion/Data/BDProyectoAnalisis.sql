-- Crear base de datos si no existe
CREATE DATABASE SistemaDonacionDb

USE SistemaDonacionDb

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

select * from dbo.Usuarios

UPDATE dbo.Usuarios 
SET Contrasenia = '$PBKDF2$10000$0gzEsT3NxZ8qkwrasX+jOQ==$r50LU135a5uw+U3ThW9f1jdtwYisF2Cr/3AM+iBCvQE=' 
WHERE Nombre = 'admin'

-- Verificar
SELECT * FROM dbo.Usuarios WHERE Nombre = 'admin'

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

-- helper para crear usuarios en la api
/*
--http://localhost:5000/api/helper/generate-hash?password=Medico123!
*/