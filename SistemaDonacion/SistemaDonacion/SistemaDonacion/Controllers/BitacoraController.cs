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

                return Ok(new
                {
                    success = true,
                    data = bitacoras,
                    total = bitacoras.Count,
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
    }
}
