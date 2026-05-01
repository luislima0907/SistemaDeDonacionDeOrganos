namespace SistemaDonacion.DTOs
{
    // Filtros que manda el frontend (fechas, tabla, acción, página)
    public class BitacoraFiltroDto
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? Tabla { get; set; }
        public string? Accion { get; set; }
        public int? UsuarioId { get; set; }
        public int Pagina { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    // Un registro de bitácora que se manda al frontend
    public class BitacoraResponseDto
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string RolUsuario { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public string Tabla { get; set; } = string.Empty;
        public int RegistroId { get; set; }
        public string? DatosAnteriores { get; set; }
        public string? DatosNuevos { get; set; }
        public string? DetallesCambios { get; set; }
        public string? IPAddress { get; set; }
        public DateTime FechaAccion { get; set; }
        public string? Detalles { get; set; }
    }

    // Lista paginada con métricas incluidas para las tarjetas del dashboard
    public class BitacoraPaginadaDto
    {
        public List<BitacoraResponseDto> Data { get; set; } = new();
        public int Total { get; set; }
        public int Pagina { get; set; }
        public int PageSize { get; set; }
        public int TotalPaginas { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int TotalCrear { get; set; }
        public int TotalActualizar { get; set; }
        public int TotalEliminar { get; set; }
    }

    // Una fila del resumen estadístico (tab de resumen)
    public class BitacoraResumenDto
    {
        public string Tabla { get; set; } = string.Empty;
        public string Accion { get; set; } = string.Empty;
        public int TotalAcciones { get; set; }
        public int UsuariosInvolucrados { get; set; }
        public int RegistrosAfectados { get; set; }
        public DateTime PrimeraAccion { get; set; }
        public DateTime UltimaAccion { get; set; }
    }
}
