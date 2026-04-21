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
    public class DonanteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBitacoraService _bitacora;

        public DonanteController(AppDbContext context, IBitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // GET: api/donante - Obtener todos los donantes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDonantes()
        {
            var donantes = await _context.Donantes
                .Include(d => d.Hospital)
                .ToListAsync();

            // Retornar sin órganos para evitar ciclos
            var resultado = donantes.Select(d => new
            {
                d.Id,
                d.Nombre,
                d.TipoSanguineo,
                d.Edad,
                d.FechaRegistro,
                d.Estado,
                d.HospitalId,
                d.Observaciones,
                d.FechaActualizacion,
                Hospital = d.Hospital != null ? new { d.Hospital.Id, d.Hospital.Nombre, d.Hospital.Ciudad } : null,
                Organos = d.Organos.Select(o => new { o.Id, o.TipoOrgano, o.Estado, o.FechaDisponibilidad })
            }).ToList();

            return Ok(resultado);
        }

        // GET: api/donante/{id} - Obtener un donante específico
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDonante(int id)
        {
            var donante = await _context.Donantes
                .Include(d => d.Hospital)
                .Include(d => d.Organos)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (donante == null)
                return NotFound(new { mensaje = "Donante no encontrado" });

            var resultado = new
            {
                donante.Id,
                donante.Nombre,
                donante.TipoSanguineo,
                donante.Edad,
                donante.FechaRegistro,
                donante.Estado,
                donante.HospitalId,
                donante.Observaciones,
                donante.FechaActualizacion,
                Hospital = donante.Hospital != null ? new { donante.Hospital.Id, donante.Hospital.Nombre, donante.Hospital.Ciudad } : null,
                Organos = donante.Organos.Select(o => new { o.Id, o.TipoOrgano, o.Estado, o.FechaDisponibilidad })
            };

            return Ok(resultado);
        }

        // POST: api/donante - Registrar un nuevo donante
        [HttpPost]
        public async Task<ActionResult<object>> CrearDonante([FromBody] CreateDonanteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Obtener el usuario autenticado
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (usuarioId == 0)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            // Validar datos obligatorios
            if (string.IsNullOrWhiteSpace(request.Nombre) || 
                string.IsNullOrWhiteSpace(request.TipoSanguineo) || 
                request.Edad <= 0)
                return BadRequest(new { mensaje = "Datos incompletos o inválidos" });

            // Validar que el hospital exista
            var hospitalExiste = await _context.Hospitales.AnyAsync(h => h.Id == request.HospitalId && h.Estado);
            if (!hospitalExiste)
                return BadRequest(new { mensaje = "Hospital inválido o inactivo" });

            var donante = new Donante
            {
                Nombre = request.Nombre.Trim(),
                TipoSanguineo = request.TipoSanguineo.ToUpper().Trim(),
                Edad = request.Edad,
                HospitalId = request.HospitalId,
                Estado = "Disponible",
                Observaciones = request.Observaciones?.Trim(),
                FechaRegistro = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            _context.Donantes.Add(donante);
            await _context.SaveChangesAsync();

            // Cargar relaciones
            await _context.Entry(donante).Reference(d => d.Hospital).LoadAsync();
            await _context.Entry(donante).Collection(d => d.Organos).LoadAsync();

            // Registrar en bitácora
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "Registrar Donante",
                    "Donantes",
                    donante.Id,
                    null,
                    $"Donante: {donante.Nombre}, Tipo Sanguíneo: {donante.TipoSanguineo}",
                    "Nuevo donante registrado en el sistema"
                );
            }
            catch
            {
                // Si falla la bitácora, no afecta el registro del donante
            }

            var resultado = new
            {
                donante.Id,
                donante.Nombre,
                donante.TipoSanguineo,
                donante.Edad,
                donante.FechaRegistro,
                donante.Estado,
                donante.HospitalId,
                donante.Observaciones,
                donante.FechaActualizacion,
                Hospital = donante.Hospital != null ? new { donante.Hospital.Id, donante.Hospital.Nombre, donante.Hospital.Ciudad } : null,
                Organos = donante.Organos.Select(o => new { o.Id, o.TipoOrgano, o.Estado, o.FechaDisponibilidad })
            };

            return CreatedAtAction(nameof(GetDonante), new { id = donante.Id }, resultado);
        }

        // PUT: api/donante/{id}/estado - Actualizar estado del donante
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstadoDonante(int id, [FromBody] UpdateEstadoRequest request)
        {
            var donante = await _context.Donantes.FindAsync(id);
            if (donante == null)
                return NotFound(new { mensaje = "Donante no encontrado" });

            // Validar cambio de estado
            var estadosValidos = new[] { "Disponible", "Asignado", "Rechazado" };
            if (!estadosValidos.Contains(request.NuevoEstado))
                return BadRequest(new { mensaje = "Estado inválido" });

            var estadoAnterior = donante.Estado;
            donante.Estado = request.NuevoEstado;
            donante.FechaActualizacion = DateTime.Now;

            _context.Donantes.Update(donante);
            await _context.SaveChangesAsync();

            // Registrar en bitácora
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "Actualizar Estado Donante",
                    "Donantes",
                    donante.Id,
                    $"Estado anterior: {estadoAnterior}",
                    $"Nuevo estado: {donante.Estado}",
                    request.Observaciones?.Trim()
                );
            }
            catch
            {
                // Si falla la bitácora, no afecta la actualización
            }

            return Ok(new { mensaje = "Estado actualizado correctamente", donante });
        }

        // GET: api/donante/{donanteId}/organos - Obtener órganos de un donante
        [HttpGet("{donanteId}/organos")]
        public async Task<ActionResult<IEnumerable<Organo>>> GetOrganosPorDonante(int donanteId)
        {
            var donante = await _context.Donantes.FindAsync(donanteId);
            if (donante == null)
                return NotFound(new { mensaje = "Donante no encontrado" });

            var organos = await _context.Organos
                .Where(o => o.DonanteId == donanteId)
                .ToListAsync();

            return Ok(organos);
        }
    }

    public class CreateDonanteRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string TipoSanguineo { get; set; } = string.Empty;
        public int Edad { get; set; }
        public int HospitalId { get; set; }
        public string? Observaciones { get; set; }
    }

    public partial class UpdateEstadoRequest
    {
        public string NuevoEstado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}
