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
        private readonly IRankingService _rankingService;

        public OrganoController(AppDbContext context, IBitacoraService bitacora, IRankingService rankingService)
        {
            _context = context;
            _bitacora = bitacora;
            _rankingService = rankingService;
        }
        private int? ObtenerHospitalIdDelUsuario()
        {
            var valor = User.FindFirst("HospitalId")?.Value;
            if (int.TryParse(valor, out var id) && id > 0)
                return id;
            return null;
        }

        // GET: api/organo - Obtener todos los órganos
        [HttpGet]
public async Task<ActionResult<IEnumerable<object>>> GetOrganos()
{
    var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
    IQueryable<Organo> query = _context.Organos.Include(o => o.Donante);

    if (!rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
        !rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        var hospitalId = ObtenerHospitalIdDelUsuario();
        if (hospitalId == null)
            return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });

        // Filtrar por hospital del donante
        query = query.Where(o => o.Donante != null && o.Donante.HospitalId == hospitalId.Value);
    }

    var organos = await query.ToListAsync();

    var resultado = organos.Select(o => new
    {
        o.Id, o.DonanteId, o.TipoOrgano, o.Estado,
        o.FechaDisponibilidad, o.Compatibilidad, o.FechaActualizacion,
        Donante = o.Donante != null ? new
        {
            o.Donante.Id, o.Donante.Nombre,
            o.Donante.TipoSanguineo, o.Donante.Edad, o.Donante.Estado
        } : null
    }).ToList();

    return Ok(resultado);
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

        // GET: api/organo/disponibles - Obtener conteo de órganos disponibles
        [HttpGet("disponibles")]
        public async Task<ActionResult<object>> GetOrganosDisponibles()
        {
            try
            {
                var count = await _context.Organos
                    .Where(o => o.Estado == "Disponible")
                    .CountAsync();

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // GET: api/organo/ranking-tipo/{tipoOrgano}/{tipoSanguineo} - Obtener ranking por tipo de órgano
        [HttpGet("ranking-tipo/{tipoOrgano}/{tipoSanguineo}")]
        public async Task<ActionResult<List<RankingPrioridad>>> ObtenerRankingPorTipo(string tipoOrgano, string tipoSanguineo)
        {
            if (string.IsNullOrWhiteSpace(tipoOrgano) || string.IsNullOrWhiteSpace(tipoSanguineo))
                return BadRequest(new { mensaje = "Tipo de órgano y tipo de sangre son requeridos" });

            var ranking = await _rankingService.ObtenerRankingPrioridadPorTipoOrganoAsync(tipoOrgano.Trim(), tipoSanguineo.Trim());

            if (ranking.Count == 0)
                return Ok(new { mensaje = "No hay pacientes compatibles disponibles", datos = ranking });

            return Ok(ranking);
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

        // GET: api/organo/{id}/ranking - Obtener ranking de prioridad para un órgano específico
        [HttpGet("{id}/ranking")]
        public async Task<ActionResult<List<RankingPrioridad>>> ObtenerRankingPrioridad(int id)
        {
            try
            {
                var organo = await _context.Organos
                    .Include(o => o.Donante)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (organo == null)
                    return NotFound(new { mensaje = "Órgano no encontrado" });

                if (organo.Estado != "Disponible")
                    return BadRequest(new { mensaje = "El órgano debe estar disponible para obtener el ranking" });

                // Validar que el donante existe
                if (organo.Donante == null)
                    return BadRequest(new { mensaje = "El órgano no tiene donante asociado" });

                var ranking = await _rankingService.ObtenerRankingPrioridadAsync(id);

                // Registrar consulta en bitácora
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                try
                {
                    await _bitacora.RegistrarAccionAsync(
                        usuarioId,
                        "Consultar Ranking Prioridad",
                        "Organos",
                        id,
                        $"Órgano: {organo.TipoOrgano}",
                        $"Pacientes compatibles encontrados: {ranking.Count}",
                        "Ranking de prioridad consultado"
                    );
                }
                catch
                {
                    // Si falla la bitácora, no afecta la consulta
                }

                return Ok(ranking);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR OrganoController.ObtenerRankingPrioridad] {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { mensaje = "Error al obtener el ranking", detalle = ex.Message });
            }
        }

        // GET: api/organo/{id}/debug-ranking - DEBUG: Ver qué pacientes se están filtrando
        [HttpGet("{id}/debug-ranking")]
        public async Task<ActionResult<object>> DebugRankingPrioridad(int id)
        {
            try
            {
                var organo = await _context.Organos
                    .Include(o => o.Donante)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (organo == null)
                    return NotFound(new { mensaje = "Órgano no encontrado" });

                if (organo.Donante == null)
                    return BadRequest(new { mensaje = "El órgano no tiene donante" });

                var tipoOrgano = organo.TipoOrgano?.Trim() ?? "";
                var tipoSanguineoDonante = organo.Donante.TipoSanguineo?.Trim() ?? "";

                // Obtener TODOS los pacientes
                var todosPacientes = await _context.Pacientes.ToListAsync();

                // Filtrar por tipo de órgano
                var porOrgano = todosPacientes.Where(p => p.OrganoRequerido?.Trim() == tipoOrgano).ToList();

                // Filtrar por estado
                var porEstado = porOrgano.Where(p => p.Estado == "Activo").ToList();

                // Filtrar por antigüedad
                var ahora = DateTime.Now;
                var fechaLimite = ahora.AddDays(-30);
                var porAntiguedad = porEstado.Where(p => p.FechaActualizacion >= fechaLimite).ToList();

                // Verificar compatibilidad
                var porCompatibilidad = new List<object>();
                foreach (var paciente in porAntiguedad)
                {
                    var tipoSanguineoPaciente = paciente.TipoSanguineo?.Trim() ?? "";
                    var esCompatible = EsCompatibleSanguineDebug(tipoSanguineoPaciente, tipoSanguineoDonante);
                    porCompatibilidad.Add(new
                    {
                        paciente.Id,
                        paciente.Nombre,
                        TipoSanguineoPaciente = tipoSanguineoPaciente,
                        TipoSanguineoDonante = tipoSanguineoDonante,
                        EsCompatible = esCompatible
                    });
                }

                return Ok(new
                {
                    debug = true,
                    organo = new { organo.Id, organo.TipoOrgano, Donante = new { organo.Donante.Nombre, organo.Donante.TipoSanguineo } },
                    filtros = new
                    {
                        totalPacientes = todosPacientes.Count,
                        porOrgano = porOrgano.Count,
                        porEstado = porEstado.Count,
                        porAntiguedad = porAntiguedad.Count,
                        porCompatibilidad = porCompatibilidad.Count
                    },
                    pacientesCompatibles = porCompatibilidad,
                    todosPacientes = todosPacientes.Select(p => new
                    {
                        p.Id,
                        p.Nombre,
                        p.TipoSanguineo,
                        p.OrganoRequerido,
                        p.NivelUrgencia,
                        p.Estado,
                        p.FechaActualizacion
                    }).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error en debug", error = ex.Message, stackTrace = ex.StackTrace });
            }
        }

        // Método auxiliar para debug
        private bool EsCompatibleSanguineDebug(string tipoReceptor, string tipoDonanteAsync)
        {
            var compatibilidad = new Dictionary<string, List<string>>
            {
                { "O+", new List<string> { "O+", "O-" } },
                { "O-", new List<string> { "O-" } },
                { "A+", new List<string> { "A+", "A-", "O+", "O-" } },
                { "A-", new List<string> { "A-", "O-" } },
                { "B+", new List<string> { "B+", "B-", "O+", "O-" } },
                { "B-", new List<string> { "B-", "O-" } },
                { "AB+", new List<string> { "AB+", "AB-", "A+", "A-", "B+", "B-", "O+", "O-" } },
                { "AB-", new List<string> { "AB-", "A-", "B-", "O-" } }
            };

            if (!compatibilidad.TryGetValue(tipoReceptor, out var donantesCompatibles))
                return false;

            return donantesCompatibles.Contains(tipoDonanteAsync);
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
            
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                !rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var hospitalId = ObtenerHospitalIdDelUsuario();
                if (hospitalId == null)
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado." });

                if (donante.HospitalId != hospitalId.Value)
                    return StatusCode(403, new { mensaje = "No tiene permisos para registrar órganos de donantes de otro hospital" });
            }

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
