using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Services;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BitacoraController : ControllerBase
    {
        private readonly IBitacoraService _bitacoraService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public BitacoraController(IBitacoraService bitacoraService, IHttpContextAccessor httpContextAccessor)
        {
            _bitacoraService = bitacoraService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Obtiene el historial de cambios de un registro específico
        /// </summary>
        [HttpGet("historial/{tabla}/{registroId}")]
        public async Task<IActionResult> ObtenerHistorialRegistro(string tabla, int registroId)
        {
            try
            {
                var historial = await _bitacoraService.ObtenerHistorialRegistroAsync(tabla, registroId);
                return Ok(new
                {
                    success = true,
                    data = historial,
                    message = $"Historial de {tabla} (ID: {registroId}) obtenido exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener historial: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene las bitácoras de un usuario específico
        /// </summary>
        [HttpGet("usuario/{usuarioId}")]
        public async Task<IActionResult> ObtenerBitacorasUsuario(int usuarioId,
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                var bitacoras = await _bitacoraService.ObtenerBitacorasPorUsuarioAsync(usuarioId, fechaInicio, fechaFin);
                return Ok(new
                {
                    success = true,
                    data = bitacoras,
                    total = bitacoras.Count,
                    message = "Bitácoras del usuario obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene las bitácoras recientes de los últimos N días
        /// </summary>
        [HttpGet("recientes")]
        public async Task<IActionResult> ObtenerBitacorasRecientes([FromQuery] int dias = 7)
        {
            try
            {
                if (dias < 1 || dias > 365)
                    return BadRequest(new { success = false, message = "Los días deben estar entre 1 y 365" });

                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(dias);
                return Ok(new
                {
                    success = true,
                    data = bitacoras,
                    total = bitacoras.Count,
                    dias = dias,
                    message = $"Bitácoras de los últimos {dias} días obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras recientes: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene un resumen estadístico de la bitácora
        /// </summary>
        [HttpGet("resumen")]
        public async Task<IActionResult> ObtenerResumenBitacora(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null)
        {
            try
            {
                // Si no se proporciona rango, usar los últimos 30 días
                fechaInicio = fechaInicio ?? DateTime.Now.AddDays(-30);
                fechaFin = fechaFin ?? DateTime.Now;

                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(
                    (int)(DateTime.Now - fechaInicio.Value).TotalDays);

                var resumen = bitacoras
                    .GroupBy(b => new { b.Tabla, b.Accion })
                    .Select(g => new
                    {
                        tabla = g.Key.Tabla,
                        accion = g.Key.Accion,
                        totalAcciones = g.Count(),
                        usuariosInvolucrados = g.Select(b => b.UsuarioId).Distinct().Count(),
                        registrosAfectados = g.Select(b => b.RegistroId).Distinct().Count(),
                        primeraAccion = g.Min(b => b.FechaAccion),
                        ultimaAccion = g.Max(b => b.FechaAccion)
                    })
                    .OrderBy(r => r.tabla)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = resumen,
                    totalRegistros = bitacoras.Count,
                    fechaInicio = fechaInicio.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    fechaFin = fechaFin.Value.ToString("yyyy-MM-dd HH:mm:ss"),
                    message = "Resumen de bitácora obtenido exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener resumen: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene todas las bitácoras (solo admin)
        /// </summary>
        [HttpGet("todas")]
        public async Task<IActionResult> ObtenerTodasBitacoras(
            [FromQuery] int dias = 30,
            [FromQuery] string? tabla = null,
            [FromQuery] string? accion = null)
        {
            try
            {
                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(dias);

                if (!string.IsNullOrEmpty(tabla))
                    bitacoras = bitacoras.Where(b => b.Tabla.Contains(tabla, StringComparison.OrdinalIgnoreCase)).ToList();

                if (!string.IsNullOrEmpty(accion))
                    bitacoras = bitacoras.Where(b => b.Accion.Contains(accion, StringComparison.OrdinalIgnoreCase)).ToList();

                // Calcular conteos por acción (usando Contains para más flexibilidad)
                var totalCrear = bitacoras.Count(b =>
                    b.Accion != null && (b.Accion.Contains("Registrar", StringComparison.OrdinalIgnoreCase) ||
                    b.Accion.Equals("Registrar", StringComparison.OrdinalIgnoreCase)));

                var totalActualizar = bitacoras.Count(b =>
                    b.Accion != null && (b.Accion.Contains("Actualizar", StringComparison.OrdinalIgnoreCase) ||
                    b.Accion.Equals("Actualizar", StringComparison.OrdinalIgnoreCase)));

                var totalEliminar = bitacoras.Count(b =>
                    b.Accion != null && (b.Accion.Contains("Eliminar", StringComparison.OrdinalIgnoreCase) ||
                    b.Accion.Equals("Eliminar", StringComparison.OrdinalIgnoreCase)));

                return Ok(new
                {
                    success = true,
                    data = bitacoras,
                    total = bitacoras.Count,
                    totalRegistrar = totalCrear,
                    totalActualizar = totalActualizar,
                    totalEliminar = totalEliminar,
                    filtros = new { dias, tabla, accion },
                    message = "Todas las bitácoras obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras: {ex.Message}"
                });
            }
        }

        // Nuevo endpoint para filtrado avanzado y paginado desde frontend
        [HttpGet("filtrada")]
        public async Task<IActionResult> ObtenerBitacorasFiltrada(
            [FromQuery] DateTime? fechaInicio = null,
            [FromQuery] DateTime? fechaFin = null,
            [FromQuery] string? tabla = null,
            [FromQuery] string? accion = null,
            [FromQuery] int pagina = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                // Construir DTO de filtro (definido en los servicios)
                var filtro = new SistemaDonacion.DTOs.BitacoraFiltroDto
                {
                    FechaInicio = fechaInicio,
                    FechaFin = fechaFin,
                    Tabla = tabla,
                    Accion = accion,
                    Pagina = pagina,
                    PageSize = pageSize
                };

                var resultado = await _bitacoraService.ObtenerBitacorasFiltradaAsync(filtro);

                return Ok(new
                {
                    success = true,
                    data = resultado.Data,
                    total = resultado.Total,
                    pagina = resultado.Pagina,
                    pageSize = resultado.PageSize,
                    totalPaginas = resultado.TotalPaginas,
                    fechaInicio = resultado.FechaInicio.ToString("yyyy-MM-dd HH:mm:ss"),
                    fechaFin = resultado.FechaFin.ToString("yyyy-MM-dd HH:mm:ss"),
                    totalRegistrar = resultado.TotalCrear,
                    totalActualizar = resultado.TotalActualizar,
                    totalEliminar = resultado.TotalEliminar,
                    message = "Bitácoras filtradas obtenidas exitosamente"
                });
            }
            catch (ArgumentException argEx)
            {
                return BadRequest(new { success = false, message = argEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener bitácoras filtradas: {ex.Message}"
                });
            }
        }

        // Nuevo endpoint para obtener tablas y acciones disponibles
        [HttpGet("opciones")]
        public async Task<IActionResult> ObtenerOpcionesDisponibles()
        {
            try
            {
                var bitacoras = await _bitacoraService.ObtenerBitacorasRecentesAsync(365);

                var tablas = bitacoras
                    .Select(b => b.Tabla)
                    .Distinct()
                    .OrderBy(t => t)
                    .ToList();

                var acciones = bitacoras
                    .Select(b => b.Accion)
                    .Distinct()
                    .OrderBy(a => a)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    tablas = tablas,
                    acciones = acciones,
                    message = "Opciones obtenidas exitosamente"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Error al obtener opciones: {ex.Message}"
                });
            }
        }
    }
}
