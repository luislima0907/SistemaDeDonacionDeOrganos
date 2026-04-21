using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Data;
using SistemaDonacion.Models;
using SistemaDonacion.Services;
using System.Security.Claims;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrganoController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBitacoraService _bitacora;

        public OrganoController(AppDbContext context, IBitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // GET: api/organo - Obtener todos los órganos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetOrganos()
        {
            var organos = await _context.Organos
                .Include(o => o.Donante)
                .ToListAsync();

            var resultado = organos.Select(o => new
            {
                o.Id,
                o.DonanteId,
                o.TipoOrgano,
                o.Estado,
                o.FechaDisponibilidad,
                o.Compatibilidad,
                o.FechaActualizacion,
                Donante = o.Donante != null ? new
                {
                    o.Donante.Id,
                    o.Donante.Nombre,
                    o.Donante.TipoSanguineo,
                    o.Donante.Edad,
                    o.Donante.Estado
                } : null
            }).ToList();

            return Ok(resultado);
        }

        // GET: api/organo/{id} - Obtener un órgano específico
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetOrgano(int id)
        {
            var organo = await _context.Organos
                .Include(o => o.Donante)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (organo == null)
                return NotFound(new { mensaje = "Órgano no encontrado" });

            var resultado = new
            {
                organo.Id,
                organo.DonanteId,
                organo.TipoOrgano,
                organo.Estado,
                organo.FechaDisponibilidad,
                organo.Compatibilidad,
                organo.FechaActualizacion,
                Donante = organo.Donante != null ? new
                {
                    organo.Donante.Id,
                    organo.Donante.Nombre,
                    organo.Donante.TipoSanguineo,
                    organo.Donante.Edad,
                    organo.Donante.Estado
                } : null
            };

            return Ok(resultado);
        }

        // POST: api/organo - Registrar un nuevo órgano
        [HttpPost]
        public async Task<ActionResult<object>> CrearOrgano([FromBody] CreateOrganoRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Obtener el usuario autenticado
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (usuarioId == 0)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            // Validar que el donante existe
            var donante = await _context.Donantes.FindAsync(request.DonanteId);
            if (donante == null)
                return NotFound(new { mensaje = "Donante no encontrado" });

            // Validar datos obligatorios
            if (string.IsNullOrWhiteSpace(request.TipoOrgano))
                return BadRequest(new { mensaje = "Tipo de órgano es obligatorio" });

            // Validar que no exista un órgano duplicado del mismo tipo
            var organoExistente = await _context.Organos
                .Where(o => o.DonanteId == request.DonanteId && 
                            o.TipoOrgano == request.TipoOrgano.Trim() && 
                            o.Estado == "Disponible")
                .FirstOrDefaultAsync();

            if (organoExistente != null)
                return BadRequest(new { mensaje = "Este donante ya tiene un órgano de este tipo disponible" });

            var organo = new Organo
            {
                DonanteId = request.DonanteId,
                TipoOrgano = request.TipoOrgano.Trim(),
                Estado = "Disponible",
                FechaDisponibilidad = DateTime.Now,
                Compatibilidad = request.Compatibilidad?.Trim(),
                FechaActualizacion = DateTime.Now
            };

            _context.Organos.Add(organo);
            await _context.SaveChangesAsync();

            // Registrar en bitácora
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "Registrar Órgano",
                    "Organos",
                    organo.Id,
                    null,
                    $"Órgano: {organo.TipoOrgano}, Donante: {donante.Nombre}",
                    "Nuevo órgano registrado en el sistema"
                );
            }
            catch
            {
                // Si falla la bitácora, no afecta el registro del órgano
            }

            // Recargar donante para retornar
            await _context.Entry(organo).Reference(o => o.Donante).LoadAsync();

            var resultado = new
            {
                organo.Id,
                organo.DonanteId,
                organo.TipoOrgano,
                organo.Estado,
                organo.FechaDisponibilidad,
                organo.Compatibilidad,
                organo.FechaActualizacion,
                Donante = organo.Donante != null ? new
                {
                    organo.Donante.Id,
                    organo.Donante.Nombre,
                    organo.Donante.TipoSanguineo,
                    organo.Donante.Edad,
                    organo.Donante.Estado
                } : null
            };

            return CreatedAtAction(nameof(GetOrgano), new { id = organo.Id }, resultado);
        }

        // PUT: api/organo/{id}/estado - Actualizar estado del órgano
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstadoOrgano(int id, [FromBody] UpdateOrganoEstadoRequest request)
        {
            var organo = await _context.Organos.FindAsync(id);
            if (organo == null)
                return NotFound(new { mensaje = "Órgano no encontrado" });

            // Validar cambio de estado
            var estadosValidos = new[] { "Disponible", "Asignado", "Descartado", "Trasplantado" };
            if (!estadosValidos.Contains(request.NuevoEstado))
                return BadRequest(new { mensaje = "Estado inválido" });

            var estadoAnterior = organo.Estado;
            organo.Estado = request.NuevoEstado;
            organo.FechaActualizacion = DateTime.Now;

            _context.Organos.Update(organo);
            await _context.SaveChangesAsync();

            // Registrar en bitácora
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "Actualizar Estado Órgano",
                    "Organos",
                    organo.Id,
                    $"Estado anterior: {estadoAnterior}",
                    $"Nuevo estado: {organo.Estado}",
                    request.Observaciones?.Trim()
                );
            }
            catch
            {
                // Si falla la bitácora, no afecta la actualización
            }

            return Ok(new { mensaje = "Estado actualizado correctamente", organo });
        }

        // GET: api/organo/disponibles/{tipoOrgano} - Obtener órganos disponibles por tipo
        [HttpGet("disponibles/{tipoOrgano}")]
        public async Task<ActionResult<IEnumerable<object>>> GetOrganosDisponibles(string tipoOrgano)
        {
            var organos = await _context.Organos
                .Include(o => o.Donante)
                .Where(o => o.TipoOrgano == tipoOrgano.Trim() && o.Estado == "Disponible")
                .ToListAsync();

            var resultado = organos.Select(o => new
            {
                o.Id,
                o.DonanteId,
                o.TipoOrgano,
                o.Estado,
                o.FechaDisponibilidad,
                o.Compatibilidad,
                o.FechaActualizacion,
                Donante = o.Donante != null ? new
                {
                    o.Donante.Id,
                    o.Donante.Nombre,
                    o.Donante.TipoSanguineo,
                    o.Donante.Edad,
                    o.Donante.Estado
                } : null
            }).ToList();

            return Ok(resultado);
        }

        // DELETE: api/organo/{id} - Eliminar un órgano (solo si está disponible)
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarOrgano(int id)
        {
            var organo = await _context.Organos.FindAsync(id);
            if (organo == null)
                return NotFound(new { mensaje = "Órgano no encontrado" });

            // Solo se puede eliminar si está disponible
            if (organo.Estado != "Disponible")
                return BadRequest(new { mensaje = "Solo se pueden eliminar órganos disponibles" });

            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var donanteId = organo.DonanteId;

            _context.Organos.Remove(organo);
            await _context.SaveChangesAsync();

            // Registrar en bitácora
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "Eliminar Órgano",
                    "Organos",
                    id,
                    $"Órgano: {organo.TipoOrgano}, Donante ID: {donanteId}",
                    null,
                    "Órgano eliminado del sistema"
                );
            }
            catch
            {
                // Si falla la bitácora, no afecta la eliminación
            }

            return Ok(new { mensaje = "Órgano eliminado correctamente" });
        }
    }

    public class CreateOrganoRequest
    {
        public int DonanteId { get; set; }
        public string TipoOrgano { get; set; } = string.Empty;
        public string? Compatibilidad { get; set; }
    }

    public class UpdateOrganoEstadoRequest
    {
        public string NuevoEstado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}
