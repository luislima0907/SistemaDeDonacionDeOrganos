using SistemaDonacion.Data;
using SistemaDonacion.Models;
using SistemaDonacion.DTOs;
using System.Text;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;

namespace SistemaDonacion.Services
{
    public interface IBitacoraService
    {
        Task RegistrarAccionAsync(int usuarioId, string accion, string tabla, int registroId,
            string? datosAnteriores = null, string? datosNuevos = null, string? detalles = null,
            string? ipAddress = null, string? detallesCambios = null);

        Task RegistrarCambiosAutomaticosAsync(int usuarioId, string? ipAddress = null);

        Task<List<BitacoraAccion>> ObtenerHistorialRegistroAsync(string tabla, int registroId);

        Task<List<BitacoraAccion>> ObtenerBitacorasPorUsuarioAsync(int usuarioId, DateTime? fechaInicio = null, DateTime? fechaFin = null);

        Task<List<BitacoraAccion>> ObtenerBitacorasRecentesAsync(int dias = 7);

        Task<BitacoraPaginadaDto> ObtenerBitacorasFiltradaAsync(BitacoraFiltroDto filtro);

        Task<List<BitacoraResumenDto>> ObtenerResumenAsync(DateTime fechaInicio, DateTime fechaFin);

        Task<byte[]> ExportarExcelAsync(BitacoraFiltroDto filtro);

        Task<byte[]> ExportarPdfAsync(BitacoraFiltroDto filtro);
    }

    public class BitacoraService : IBitacoraService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BitacoraService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RegistrarAccionAsync(int usuarioId, string accion, string tabla,
            int registroId, string? datosAnteriores = null, string? datosNuevos = null,
            string? detalles = null, string? ipAddress = null, string? detallesCambios = null)
        {
            try
            {
                var bitacora = new BitacoraAccion
                {
                    UsuarioId = usuarioId,
                    Accion = accion,
                    Tabla = tabla,
                    RegistroId = registroId,
                    DatosAnteriores = datosAnteriores,
                    DatosNuevos = datosNuevos,
                    Detalles = detalles,
                    IPAddress = ipAddress ?? ObtenerIPCliente(),
                    DetallesCambios = detallesCambios,
                    FechaAccion = DateTime.Now
                };

                _context.BitacoraAcciones.Add(bitacora);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar bitácora: {ex.Message}");
            }
        }

        public async Task RegistrarCambiosAutomaticosAsync(int usuarioId, string? ipAddress = null)
        {
            try
            {
                var cambios = _context.ChangeTracker.Entries()
                    .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)
                    .ToList();

                foreach (var cambio in cambios)
                {
                    var entity = cambio.Entity;
                    var tipoEntidad = entity.GetType().Name;

                    if (!DebeRegistrarseEnBitacora(tipoEntidad))
                        continue;

                    int? registroId = ObtenerIdEntidad(entity);
                    if (registroId == null)
                        continue;

                    string accion = cambio.State switch
                    {
                        EntityState.Added => "Crear",
                        EntityState.Modified => "Actualizar",
                        EntityState.Deleted => "Eliminar",
                        _ => "Cambio"
                    };

                    var datosAnteriores = cambio.State == EntityState.Modified ? SerializarEntidad(cambio.OriginalValues) : null;
                    var datosNuevos = cambio.State != EntityState.Deleted ? SerializarEntidad(cambio.CurrentValues) : null;
                    var detallesCambios = GenerarDetallesCambios(cambio);

                    var bitacora = new BitacoraAccion
                    {
                        UsuarioId = usuarioId,
                        Accion = accion,
                        Tabla = tipoEntidad,
                        RegistroId = registroId.Value,
                        DatosAnteriores = datosAnteriores,
                        DatosNuevos = datosNuevos,
                        DetallesCambios = detallesCambios,
                        IPAddress = ipAddress ?? ObtenerIPCliente(),
                        FechaAccion = DateTime.Now,
                        Detalles = $"Cambio automático en {tipoEntidad} (ID: {registroId})"
                    };

                    _context.BitacoraAcciones.Add(bitacora);
                }

                if (cambios.Any())
                    await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al registrar cambios automáticos: {ex.Message}");
            }
        }

        public async Task<List<BitacoraAccion>> ObtenerHistorialRegistroAsync(string tabla, int registroId)
        {
            return await _context.BitacoraAcciones
                .Where(b => b.Tabla == tabla && b.RegistroId == registroId)
                .Include(b => b.Usuario)
                .OrderByDescending(b => b.FechaAccion)
                .ToListAsync();
        }

        public async Task<List<BitacoraAccion>> ObtenerBitacorasPorUsuarioAsync(
            int usuarioId, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var query = _context.BitacoraAcciones
                .Include(b => b.Usuario)
                .Where(b => b.UsuarioId == usuarioId)
                .AsQueryable();

            if (fechaInicio.HasValue)
                query = query.Where(b => b.FechaAccion >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(b => b.FechaAccion <= fechaFin.Value);

            return await query.OrderByDescending(b => b.FechaAccion).ToListAsync();
        }

        public async Task<List<BitacoraAccion>> ObtenerBitacorasRecentesAsync(int dias = 7)
        {
            var fechaInicio = DateTime.Now.AddDays(-dias);
            return await _context.BitacoraAcciones
                .Where(b => b.FechaAccion >= fechaInicio)
                .Include(b => b.Usuario)
                .OrderByDescending(b => b.FechaAccion)
                .ToListAsync();
        }

        public async Task<BitacoraPaginadaDto> ObtenerBitacorasFiltradaAsync(BitacoraFiltroDto filtro)
        {
            var fechaFin = filtro.FechaFin ?? DateTime.Now;
            var fechaInicio = filtro.FechaInicio ?? fechaFin.AddDays(-30);

            if ((fechaFin - fechaInicio).TotalDays > 183)
                throw new ArgumentException("El rango de consulta no puede superar los 6 meses.");

            if (fechaInicio > fechaFin)
                throw new ArgumentException("La fecha inicio no puede ser mayor a la fecha fin.");

            var pageSize = Math.Min(Math.Max(filtro.PageSize, 1), 50);
            var pagina = Math.Max(filtro.Pagina, 1);

            var query = _context.BitacoraAcciones
                .Include(b => b.Usuario)
                .Where(b => b.FechaAccion >= fechaInicio && b.FechaAccion <= fechaFin)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filtro.Tabla))
                query = query.Where(b => b.Tabla == filtro.Tabla);

            if (!string.IsNullOrWhiteSpace(filtro.Accion))
                query = query.Where(b => b.Accion == filtro.Accion);

            if (filtro.UsuarioId.HasValue)
                query = query.Where(b => b.UsuarioId == filtro.UsuarioId.Value);

            var total = await query.CountAsync();

            var metricas = await query
                .GroupBy(b => b.Accion)
                .Select(g => new { Accion = g.Key, Count = g.Count() })
                .ToListAsync();

            var datos = await query
                .OrderByDescending(b => b.FechaAccion)
                .Skip((pagina - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new BitacoraResponseDto
                {
                    Id = b.Id,
                    UsuarioId = b.UsuarioId,
                    NombreUsuario = b.Usuario != null ? b.Usuario.Nombre ?? "—" : "—",
                    RolUsuario = b.Usuario != null ? b.Usuario.Rol ?? "—" : "—",
                    Accion = b.Accion,
                    Tabla = b.Tabla,
                    RegistroId = b.RegistroId,
                    DatosAnteriores = b.DatosAnteriores,
                    DatosNuevos = b.DatosNuevos,
                    DetallesCambios = b.DetallesCambios,
                    IPAddress = b.IPAddress,
                    FechaAccion = b.FechaAccion,
                    Detalles = b.Detalles
                })
                .ToListAsync();

            return new BitacoraPaginadaDto
            {
                Data = datos,
                Total = total,
                Pagina = pagina,
                PageSize = pageSize,
                TotalPaginas = (int)Math.Ceiling((double)total / pageSize),
                FechaInicio = fechaInicio,
                FechaFin = fechaFin,
                TotalCrear = metricas.Where(m => m.Accion.Contains("Registrar", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Count),
                TotalActualizar = metricas.Where(m => m.Accion.Contains("Actualizar", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Count),
                TotalEliminar = metricas.Where(m => m.Accion.Contains("Eliminar", StringComparison.OrdinalIgnoreCase)).Sum(m => m.Count),
            };
        }

        public async Task<List<BitacoraResumenDto>> ObtenerResumenAsync(DateTime fechaInicio, DateTime fechaFin)
        {
            if ((fechaFin - fechaInicio).TotalDays > 183)
                throw new ArgumentException("El rango de consulta no puede superar los 6 meses.");

            return await _context.BitacoraAcciones
                .Where(b => b.FechaAccion >= fechaInicio && b.FechaAccion <= fechaFin)
                .GroupBy(b => new { b.Tabla, b.Accion })
                .Select(g => new BitacoraResumenDto
                {
                    Tabla = g.Key.Tabla,
                    Accion = g.Key.Accion,
                    TotalAcciones = g.Count(),
                    UsuariosInvolucrados = g.Select(b => b.UsuarioId).Distinct().Count(),
                    RegistrosAfectados = g.Select(b => b.RegistroId).Distinct().Count(),
                    PrimeraAccion = g.Min(b => b.FechaAccion),
                    UltimaAccion = g.Max(b => b.FechaAccion)
                })
                .OrderBy(r => r.Tabla).ThenBy(r => r.Accion)
                .ToListAsync();
        }

        public async Task<byte[]> ExportarExcelAsync(BitacoraFiltroDto filtro)
        {
            var resultado = await ObtenerBitacorasFiltradaAsync(filtro);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Bitácora");

            var headers = new[] { "ID", "Fecha", "Usuario", "Rol", "Acción", "Tabla", "ID Registro", "Datos Anteriores", "Datos Nuevos", "Detalles" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            int row = 2;
            foreach (var r in resultado.Data)
            {
                worksheet.Cell(row, 1).Value = r.Id;
                worksheet.Cell(row, 2).Value = r.FechaAccion.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cell(row, 3).Value = r.NombreUsuario;
                worksheet.Cell(row, 4).Value = r.RolUsuario;
                worksheet.Cell(row, 5).Value = r.Accion;
                worksheet.Cell(row, 6).Value = r.Tabla;
                worksheet.Cell(row, 7).Value = r.RegistroId;
                worksheet.Cell(row, 8).Value = r.DatosAnteriores ?? "—";
                worksheet.Cell(row, 9).Value = r.DatosNuevos ?? "—";
                worksheet.Cell(row, 10).Value = r.Detalles ?? "—";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public async Task<byte[]> ExportarPdfAsync(BitacoraFiltroDto filtro)
        {
            var resultado = await ObtenerBitacorasFiltradaAsync(filtro);

            var sb = new StringBuilder();
            sb.AppendLine("ID,Fecha,Usuario,Rol,Accion,Tabla,RegistroId,Detalles");
            foreach (var r in resultado.Data)
                sb.AppendLine($"{r.Id},{r.FechaAccion:yyyy-MM-dd HH:mm:ss}," +
                              $"{r.NombreUsuario},{r.RolUsuario},{r.Accion}," +
                              $"{r.Tabla},{r.RegistroId},\"{r.Detalles}\"");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        // ─────────────────────────────────────────────────────────────────────
        // Métodos privados de utilidad
        // ─────────────────────────────────────────────────────────────────────

        private bool DebeRegistrarseEnBitacora(string tipoEntidad)
        {
            var entidadesAuditadas = new[] { "Donante", "Organo", "Paciente", "ApplicationUser", "Hospital" };
            return entidadesAuditadas.Contains(tipoEntidad);
        }

        private int? ObtenerIdEntidad(object entity)
        {
            var propiedad = entity.GetType().GetProperty("Id");
            if (propiedad != null && propiedad.GetValue(entity) is int id)
                return id;
            return null;
        }

        private string SerializarEntidad(PropertyValues values)
        {
            try
            {
                var diccionario = new Dictionary<string, object?>();
                foreach (var propiedad in values.Properties)
                {
                    var valor = values[propiedad];
                    if (valor != null && !propiedad.Name.Contains("Password") && !propiedad.Name.Contains("Contrasenia"))
                        diccionario[propiedad.Name] = valor;
                }
                return JsonSerializer.Serialize(diccionario);
            }
            catch
            {
                return "No serializable";
            }
        }

        private string GenerarDetallesCambios(EntityEntry entry)
        {
            if (entry.State != EntityState.Modified)
                return string.Empty;

            var cambios = new List<string>();
            foreach (var propiedad in entry.Properties)
            {
                if (propiedad.IsModified &&
                    !propiedad.Metadata.Name.Contains("Password") &&
                    !propiedad.Metadata.Name.Contains("Contrasenia"))
                {
                    cambios.Add($"{propiedad.Metadata.Name}: {propiedad.OriginalValue} → {propiedad.CurrentValue}");
                }
            }
            return string.Join("; ", cambios);
        }

        private string ObtenerIPCliente()
        {
            try
            {
                var context = _httpContextAccessor?.HttpContext;
                if (context?.Request.Headers.ContainsKey("X-Forwarded-For") == true)
                    return context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
                return context?.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            }
            catch
            {
                return "Desconocida";
            }
        }
    }
}
