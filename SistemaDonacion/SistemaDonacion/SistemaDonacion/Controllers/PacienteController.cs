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
    public class PacienteController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBitacoraService _bitacora;

        private static readonly string[] TiposSanguineosValidos = { "O+", "O-", "A+", "A-", "B+", "B-", "AB+", "AB-" };
        private static readonly string[] OrganosValidos = { "Corazón", "Pulmón", "Hígado", "Riñón", "Páncreas", "Córnea" };
        private static readonly string[] NivelesUrgenciaValidos = { "Alta", "Media", "Baja" };

        public PacienteController(AppDbContext context, IBitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // GET: api/paciente
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPacientes()
        {
            var pacientes = await _context.Pacientes
                .Include(p => p.Hospital)
                .ToListAsync();

            var resultado = pacientes.Select(p => new
            {
                p.Id,
                p.Nombre,
                p.TipoSanguineo,
                p.OrganoRequerido,
                p.NivelUrgencia,
                p.Estado,
                p.HospitalId,
                p.Observaciones,
                p.FechaRegistro,
                p.FechaActualizacion,
                Hospital = p.Hospital != null ? new { p.Hospital.Id, p.Hospital.Nombre, p.Hospital.Ciudad } : null
            });

            return Ok(resultado);
        }

        // GET: api/paciente/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPaciente(int id)
        {
            var paciente = await _context.Pacientes
                .Include(p => p.Hospital)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paciente == null)
                return NotFound(new { mensaje = "Paciente no encontrado" });

            return Ok(new
            {
                paciente.Id,
                paciente.Nombre,
                paciente.TipoSanguineo,
                paciente.OrganoRequerido,
                paciente.NivelUrgencia,
                paciente.Estado,
                paciente.HospitalId,
                paciente.Observaciones,
                paciente.FechaRegistro,
                paciente.FechaActualizacion,
                Hospital = paciente.Hospital != null ? new { paciente.Hospital.Id, paciente.Hospital.Nombre, paciente.Hospital.Ciudad } : null
            });
        }

        // POST: api/paciente
        [HttpPost]
        public async Task<ActionResult<object>> CrearPaciente([FromBody] CreatePacienteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { mensaje = "Datos inválidos" });

            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (usuarioId == 0)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            // Validar campos obligatorios
            if (string.IsNullOrWhiteSpace(request.Nombre) ||
                string.IsNullOrWhiteSpace(request.TipoSanguineo) ||
                string.IsNullOrWhiteSpace(request.OrganoRequerido) ||
                string.IsNullOrWhiteSpace(request.NivelUrgencia))
                return BadRequest(new { mensaje = "Todos los campos son obligatorios" });

            // Validar tipo sanguíneo
            if (!TiposSanguineosValidos.Contains(request.TipoSanguineo.ToUpper().Trim()))
                return BadRequest(new { mensaje = "Tipo sanguíneo inválido" });

            // Validar órgano requerido
            if (!OrganosValidos.Contains(request.OrganoRequerido.Trim()))
                return BadRequest(new { mensaje = "Órgano requerido inválido" });

            // Validar nivel de urgencia
            if (!NivelesUrgenciaValidos.Contains(request.NivelUrgencia.Trim()))
                return BadRequest(new { mensaje = "Nivel de urgencia inválido" });

            // Validar hospital
            var hospitalExiste = await _context.Hospitales.AnyAsync(h => h.Id == request.HospitalId && h.Estado);
            if (!hospitalExiste)
                return BadRequest(new { mensaje = "Hospital inválido o inactivo" });

            var paciente = new Paciente
            {
                Nombre = request.Nombre.Trim(),
                TipoSanguineo = request.TipoSanguineo.ToUpper().Trim(),
                OrganoRequerido = request.OrganoRequerido.Trim(),
                NivelUrgencia = request.NivelUrgencia.Trim(),
                HospitalId = request.HospitalId,
                Estado = "Activo",
                Observaciones = request.Observaciones?.Trim(),
                FechaRegistro = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            _context.Pacientes.Add(paciente);
            await _context.SaveChangesAsync();

            await _context.Entry(paciente).Reference(p => p.Hospital).LoadAsync();

            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "Registrar Paciente",
                    "Pacientes",
                    paciente.Id,
                    null,
                    $"Paciente: {paciente.Nombre}, Tipo Sanguíneo: {paciente.TipoSanguineo}, Órgano: {paciente.OrganoRequerido}",
                    "Nuevo paciente registrado en el sistema"
                );
            }
            catch { }

            return CreatedAtAction(nameof(GetPaciente), new { id = paciente.Id }, new
            {
                paciente.Id,
                paciente.Nombre,
                paciente.TipoSanguineo,
                paciente.OrganoRequerido,
                paciente.NivelUrgencia,
                paciente.Estado,
                paciente.HospitalId,
                paciente.Observaciones,
                paciente.FechaRegistro,
                Hospital = paciente.Hospital != null ? new { paciente.Hospital.Id, paciente.Hospital.Nombre, paciente.Hospital.Ciudad } : null
            });
        }

        // PUT: api/paciente/{id}/estado
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] UpdateEstadoRequest request)
        {
            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
                return NotFound(new { mensaje = "Paciente no encontrado" });

            var estadosValidos = new[] { "Activo", "Asignado", "Inactivo" };
            if (!estadosValidos.Contains(request.NuevoEstado))
                return BadRequest(new { mensaje = "Estado inválido" });

            var estadoAnterior = paciente.Estado;
            paciente.Estado = request.NuevoEstado;
            paciente.FechaActualizacion = DateTime.Now;

            await _context.SaveChangesAsync();

            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId, "Actualizar Estado Paciente", "Pacientes",
                    paciente.Id, $"Estado anterior: {estadoAnterior}",
                    $"Nuevo estado: {paciente.Estado}", request.Observaciones?.Trim()
                );
            }
            catch { }

            return Ok(new { mensaje = "Estado actualizado correctamente", paciente });
        }
    }

    public class CreatePacienteRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string TipoSanguineo { get; set; } = string.Empty;
        public string OrganoRequerido { get; set; } = string.Empty;
        public string NivelUrgencia { get; set; } = string.Empty;
        public int HospitalId { get; set; }
        public string? Observaciones { get; set; }
    }
}