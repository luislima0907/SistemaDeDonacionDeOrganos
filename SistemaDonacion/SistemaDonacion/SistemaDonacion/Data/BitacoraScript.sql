USE SistemaDonacionDb
GO

-- Verificar y mejorar la tabla BitacoraAcciones si es necesario
IF OBJECT_ID(N'dbo.BitacoraAcciones', N'U') IS NOT NULL
BEGIN
    -- Verificar si la columna DetallesCambios existe, si no, agregarla
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'BitacoraAcciones' AND COLUMN_NAME = 'DetallesCambios'
    )
    BEGIN
        ALTER TABLE dbo.BitacoraAcciones
        ADD DetallesCambios NVARCHAR(MAX) NULL;
        PRINT 'Columna DetallesCambios agregada a BitacoraAcciones.'
    END

    -- Verificar si la columna IPAddress existe
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
        WHERE TABLE_NAME = 'BitacoraAcciones' AND COLUMN_NAME = 'IPAddress'
    )
    BEGIN
        ALTER TABLE dbo.BitacoraAcciones
        ADD IPAddress NVARCHAR(50) NULL;
        PRINT 'Columna IPAddress agregada a BitacoraAcciones.'
    END
END
ELSE
BEGIN
    -- Crear la tabla si no existe
    CREATE TABLE dbo.BitacoraAcciones (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UsuarioId INT NOT NULL,
        Accion NVARCHAR(256) NOT NULL,
        Tabla NVARCHAR(100) NOT NULL,
        RegistroId INT NOT NULL,
        DatosAnteriores NVARCHAR(MAX),
        DatosNuevos NVARCHAR(MAX),
        DetallesCambios NVARCHAR(MAX),
        IPAddress NVARCHAR(50),
        FechaAccion DATETIME NOT NULL DEFAULT GETDATE(),
        Detalles NVARCHAR(MAX),
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_BitacoraAcciones_UsuarioId ON dbo.BitacoraAcciones (UsuarioId);
    CREATE INDEX IX_BitacoraAcciones_Tabla ON dbo.BitacoraAcciones (Tabla);
    CREATE INDEX IX_BitacoraAcciones_FechaAccion ON dbo.BitacoraAcciones (FechaAccion);
    CREATE INDEX IX_BitacoraAcciones_RegistroId ON dbo.BitacoraAcciones (RegistroId);
    CREATE INDEX IX_BitacoraAcciones_Accion ON dbo.BitacoraAcciones (Accion);

    PRINT 'Tabla BitacoraAcciones creada exitosamente.'
END
GO

-- Crear tabla de auditoría de cambios eliminados (soft delete log)
IF OBJECT_ID(N'dbo.BitacoraEliminaciones', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BitacoraEliminaciones (
        Id INT PRIMARY KEY IDENTITY(1,1),
        UsuarioId INT NOT NULL,
        Tabla NVARCHAR(100) NOT NULL,
        RegistroId INT NOT NULL,
        DatosEliminados NVARCHAR(MAX) NOT NULL,
        FechaEliminacion DATETIME NOT NULL DEFAULT GETDATE(),
        IPAddress NVARCHAR(50),
        Motivo NVARCHAR(256),
        FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_BitacoraEliminaciones_UsuarioId ON dbo.BitacoraEliminaciones (UsuarioId);
    CREATE INDEX IX_BitacoraEliminaciones_Tabla ON dbo.BitacoraEliminaciones (Tabla);
    CREATE INDEX IX_BitacoraEliminaciones_FechaEliminacion ON dbo.BitacoraEliminaciones (FechaEliminacion);

    PRINT 'Tabla BitacoraEliminaciones creada exitosamente.'
END
GO

-- Crear vista para consultas de auditoría
IF OBJECT_ID(N'dbo.vw_BitacoraConUsuario', N'V') IS NOT NULL
    DROP VIEW dbo.vw_BitacoraConUsuario;
GO

CREATE VIEW dbo.vw_BitacoraConUsuario AS
SELECT 
    ba.Id,
    ba.UsuarioId,
    u.Nombre AS NombreUsuario,
    u.Rol AS RolUsuario,
    ba.Accion,
    ba.Tabla,
    ba.RegistroId,
    ba.DatosAnteriores,
    ba.DatosNuevos,
    ba.DetallesCambios,
    ba.IPAddress,
    ba.FechaAccion,
    ba.Detalles,
    DATEDIFF(DAY, ba.FechaAccion, GETDATE()) AS DiasTranscurridos
FROM dbo.BitacoraAcciones ba
LEFT JOIN dbo.Usuarios u ON ba.UsuarioId = u.Id;
GO

PRINT 'Vista vw_BitacoraConUsuario creada exitosamente.'
GO

-- Crear procedimiento almacenado para registrar acciones en la bitácora
IF OBJECT_ID(N'dbo.sp_RegistrarBitacora', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RegistrarBitacora;
GO

CREATE PROCEDURE dbo.sp_RegistrarBitacora
    @UsuarioId INT,
    @Accion NVARCHAR(256),
    @Tabla NVARCHAR(100),
    @RegistroId INT,
    @DatosAnteriores NVARCHAR(MAX) = NULL,
    @DatosNuevos NVARCHAR(MAX) = NULL,
    @DetallesCambios NVARCHAR(MAX) = NULL,
    @IPAddress NVARCHAR(50) = NULL,
    @Detalles NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    BEGIN TRY
        INSERT INTO dbo.BitacoraAcciones (
            UsuarioId,
            Accion,
            Tabla,
            RegistroId,
            DatosAnteriores,
            DatosNuevos,
            DetallesCambios,
            IPAddress,
            FechaAccion,
            Detalles
        )
        VALUES (
            @UsuarioId,
            @Accion,
            @Tabla,
            @RegistroId,
            @DatosAnteriores,
            @DatosNuevos,
            @DetallesCambios,
            @IPAddress,
            GETDATE(),
            @Detalles
        );

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS BitacoraId;
    END TRY
    BEGIN CATCH
        -- Registrar el error
        DECLARE @ErrorMessage NVARCHAR(MAX) = ERROR_MESSAGE();
        DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
        RAISERROR (@ErrorMessage, @ErrorSeverity, 1);
    END CATCH
END
GO

PRINT 'Procedimiento sp_RegistrarBitacora creado exitosamente.'
GO

-- Crear procedimiento para obtener historial de auditoría de un registro
IF OBJECT_ID(N'dbo.sp_ObtenerHistorialBitacora', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ObtenerHistorialBitacora;
GO

CREATE PROCEDURE dbo.sp_ObtenerHistorialBitacora
    @Tabla NVARCHAR(100),
    @RegistroId INT
AS
BEGIN
    SET NOCOUNT ON;
    
    SELECT 
        ba.Id,
        ba.UsuarioId,
        u.Nombre AS NombreUsuario,
        u.Rol AS RolUsuario,
        ba.Accion,
        ba.Tabla,
        ba.RegistroId,
        ba.DatosAnteriores,
        ba.DatosNuevos,
        ba.DetallesCambios,
        ba.IPAddress,
        ba.FechaAccion,
        ba.Detalles
    FROM dbo.BitacoraAcciones ba
    LEFT JOIN dbo.Usuarios u ON ba.UsuarioId = u.Id
    WHERE ba.Tabla = @Tabla AND ba.RegistroId = @RegistroId
    ORDER BY ba.FechaAccion DESC;
END
GO

PRINT 'Procedimiento sp_ObtenerHistorialBitacora creado exitosamente.'
GO

-- Crear procedimiento para obtener auditoría por usuario y rango de fechas
IF OBJECT_ID(N'dbo.sp_ObtenerBitacorasPorUsuario', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ObtenerBitacorasPorUsuario;
GO

CREATE PROCEDURE dbo.sp_ObtenerBitacorasPorUsuario
    @UsuarioId INT,
    @FechaInicio DATETIME = NULL,
    @FechaFin DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @FI DATETIME = ISNULL(@FechaInicio, CAST(GETDATE() - 30 AS DATE));
    DECLARE @FF DATETIME = ISNULL(@FechaFin, GETDATE());
    
    SELECT 
        ba.Id,
        ba.UsuarioId,
        u.Nombre AS NombreUsuario,
        u.Rol AS RolUsuario,
        ba.Accion,
        ba.Tabla,
        ba.RegistroId,
        ba.DatosAnteriores,
        ba.DatosNuevos,
        ba.DetallesCambios,
        ba.IPAddress,
        ba.FechaAccion,
        ba.Detalles
    FROM dbo.BitacoraAcciones ba
    LEFT JOIN dbo.Usuarios u ON ba.UsuarioId = u.Id
    WHERE ba.UsuarioId = @UsuarioId 
        AND ba.FechaAccion BETWEEN @FI AND @FF
    ORDER BY ba.FechaAccion DESC;
END
GO

PRINT 'Procedimiento sp_ObtenerBitacorasPorUsuario creado exitosamente.'
GO

-- Crear procedimiento para obtener resumen de cambios
IF OBJECT_ID(N'dbo.sp_ObtenerResumenBitacora', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_ObtenerResumenBitacora;
GO

CREATE PROCEDURE dbo.sp_ObtenerResumenBitacora
    @FechaInicio DATETIME = NULL,
    @FechaFin DATETIME = NULL
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @FI DATETIME = ISNULL(@FechaInicio, CAST(GETDATE() - 30 AS DATE));
    DECLARE @FF DATETIME = ISNULL(@FechaFin, GETDATE());
    
    SELECT 
        ba.Tabla,
        ba.Accion,
        COUNT(*) AS TotalAcciones,
        COUNT(DISTINCT ba.UsuarioId) AS UsuariosInvolucrados,
        COUNT(DISTINCT ba.RegistroId) AS RegistrosAfectados,
        MIN(ba.FechaAccion) AS PrimeraAccion,
        MAX(ba.FechaAccion) AS UltimaAccion
    FROM dbo.BitacoraAcciones ba
    WHERE ba.FechaAccion BETWEEN @FI AND @FF
    GROUP BY ba.Tabla, ba.Accion
    ORDER BY ba.Tabla, ba.Accion;
END
GO

PRINT 'Procedimiento sp_ObtenerResumenBitacora creado exitosamente.'
GO


