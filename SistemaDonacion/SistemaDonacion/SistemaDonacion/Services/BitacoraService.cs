using SistemaDonacion.Data;
using SistemaDonacion.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Query;

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
                // Log del error pero no se relanza para evitar interrumpir operaciones críticas
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

                    // Solo registrar cambios de entidades principales
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
                {
                    await _context.SaveChangesAsync();
                }
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

        public async Task<List<BitacoraAccion>> ObtenerBitacorasPorUsuarioAsync(int usuarioId, DateTime? fechaInicio = null, DateTime? fechaFin = null)
        {
            var query = _context.BitacoraAcciones
                .Where(b => b.UsuarioId == usuarioId)
                .Include(b => b.Usuario);

            if (fechaInicio.HasValue)
                query = (IIncludableQueryable<BitacoraAccion, ApplicationUser?>)query.Where(b => b.FechaAccion >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = (IIncludableQueryable<BitacoraAccion, ApplicationUser?>)query.Where(b => b.FechaAccion <= fechaFin.Value);

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
                    {
                        diccionario[propiedad.Name] = valor;
                    }
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
                if (propiedad.IsModified && !propiedad.Metadata.Name.Contains("Password") && 
                    !propiedad.Metadata.Name.Contains("Contrasenia"))
                {
                    var anterior = propiedad.OriginalValue;
                    var nuevo = propiedad.CurrentValue;
                    cambios.Add($"{propiedad.Metadata.Name}: {anterior} → {nuevo}");
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
                {
                    return context.Request.Headers["X-Forwarded-For"].ToString().Split(',')[0].Trim();
                }
                return context?.Connection.RemoteIpAddress?.ToString() ?? "Desconocida";
            }
            catch
            {
                return "Desconocida";
            }
        }
    }
}
