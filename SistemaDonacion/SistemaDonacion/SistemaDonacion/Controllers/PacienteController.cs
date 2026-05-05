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
        private static readonly string[] EstadosValidos = { "Activo", "Trasplantado", "Fallecido" };

        public PacienteController(AppDbContext context, IBitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // Método para obtener el HospitalId del usuario autenticado
        private int? ObtenerHospitalIdDelUsuario()
        {
            var valor = User.FindFirst("HospitalId")?.Value;
            if (int.TryParse(valor, out var id) && id > 0)
                return id;
            return null;
        }

        //Registrar en bitácora si alguien intenta ver pacientes de otro hospital
        private async Task RegistrarAccesoCruzadoAsync(int usuarioId, int pacienteId, int hospitalMedico, int hospitalPaciente)
        {
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "ACCESO_DENEGADO_HOSPITAL_CRUZADO",
                    "Pacientes",
                    pacienteId,
                    $"HospitalId del médico: {hospitalMedico}",
                    $"HospitalId del paciente: {hospitalPaciente}",
                    $"Intento de acceso cruzado entre hospitales detectado y bloqueado."
                );
            }
            catch { }
        }

        // GET: api/paciente
        //Solo devuelve pacientes del mismo hospital que el usuario
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetPacientes()
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";

            // RF-10: Admin ve todos, médico solo ve los de su hospital
            IQueryable<Paciente> query = _context.Pacientes.Include(p => p.Hospital);

            if (!rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                !rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var hospitalId = ObtenerHospitalIdDelUsuario();
                if (hospitalId == null)
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });

                query = query.Where(p => p.HospitalId == hospitalId.Value);
            }

            var pacientes = await query.ToListAsync();

            if (!pacientes.Any())
                return Ok(new
                {
                    mensaje = "No hay pacientes registrados en su hospital",
                    datos = Array.Empty<object>()
                });

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
        //Valida que el paciente sea del mismo hospital que el usuario.
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetPaciente(int id)
        {
            var hospitalId = ObtenerHospitalIdDelUsuario();
            if (hospitalId == null)
                return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });

            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var paciente = await _context.Pacientes
                .Include(p => p.Hospital)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (paciente == null)
                return NotFound(new { mensaje = "Paciente no encontrado" });

            if (paciente.HospitalId != hospitalId.Value)
            {
                await RegistrarAccesoCruzadoAsync(usuarioId, paciente.Id, hospitalId.Value, paciente.HospitalId);
                return StatusCode(403, new { mensaje = "No tiene permisos para acceder a este paciente" });
            }

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

            var hospitalId = ObtenerHospitalIdDelUsuario();
            if (hospitalId == null)
                return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });

            if (string.IsNullOrWhiteSpace(request.Nombre) ||
                string.IsNullOrWhiteSpace(request.TipoSanguineo) ||
                string.IsNullOrWhiteSpace(request.OrganoRequerido) ||
                string.IsNullOrWhiteSpace(request.NivelUrgencia))
                return BadRequest(new { mensaje = "Todos los campos son obligatorios" });

            if (!TiposSanguineosValidos.Contains(request.TipoSanguineo.ToUpper().Trim()))
                return BadRequest(new { mensaje = "Tipo sanguíneo inválido" });

            if (!OrganosValidos.Contains(request.OrganoRequerido.Trim()))
                return BadRequest(new { mensaje = "Órgano requerido inválido" });

            if (!NivelesUrgenciaValidos.Contains(request.NivelUrgencia.Trim()))
                return BadRequest(new { mensaje = "Nivel de urgencia inválido" });

            if (request.HospitalId != 0 && request.HospitalId != hospitalId.Value)
            {
                await RegistrarAccesoCruzadoAsync(usuarioId, 0, hospitalId.Value, request.HospitalId);
                return StatusCode(403, new { mensaje = "No tiene permisos para registrar pacientes en otro hospital" });
            }

            var hospitalExiste = await _context.Hospitales
                .AnyAsync(h => h.Id == hospitalId.Value && h.Estado);
            if (!hospitalExiste)
                return BadRequest(new { mensaje = "Hospital inválido o inactivo" });

            var paciente = new Paciente
            {
                Nombre = request.Nombre.Trim(),
                TipoSanguineo = request.TipoSanguineo.ToUpper().Trim(),
                OrganoRequerido = request.OrganoRequerido.Trim(),
                NivelUrgencia = request.NivelUrgencia.Trim(),
                HospitalId = hospitalId.Value,
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
        public async Task<IActionResult> ActualizarEstado(int id, [FromBody] UpdateEstadoPacienteRequest request)
        {
            var hospitalId = ObtenerHospitalIdDelUsuario();
            if (hospitalId == null)
                return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });

            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
                return NotFound(new { mensaje = "Paciente no encontrado" });

            if (paciente.HospitalId != hospitalId.Value)
            {
                await RegistrarAccesoCruzadoAsync(usuarioId, paciente.Id, hospitalId.Value, paciente.HospitalId);
                return StatusCode(403, new { mensaje = "No tiene permisos para acceder a este paciente" });
            }

            var estadosValidos = new[] { "Activo", "Asignado", "Inactivo" };
            if (!estadosValidos.Contains(request.NuevoEstado))
                return BadRequest(new { mensaje = "Estado inválido" });

            var estadoAnterior = paciente.Estado;
            paciente.Estado = request.NuevoEstado;
            paciente.FechaActualizacion = DateTime.Now;
            await _context.SaveChangesAsync();

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

        // PUT: api/paciente/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarPaciente(int id, [FromBody] UpdatePacienteRequest request)
        {
            var hospitalId = ObtenerHospitalIdDelUsuario();
            if (hospitalId == null)
                return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });

            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (usuarioId == 0)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });

            var paciente = await _context.Pacientes.FindAsync(id);
            if (paciente == null)
                return NotFound(new { mensaje = "Paciente no encontrado" });

            if (paciente.HospitalId != hospitalId.Value)
            {
                await RegistrarAccesoCruzadoAsync(usuarioId, paciente.Id, hospitalId.Value, paciente.HospitalId);
                return StatusCode(403, new { mensaje = "No tiene permisos para acceder a este paciente" });
            }

            if (!string.IsNullOrWhiteSpace(request.Estado) && !EstadosValidos.Contains(request.Estado.Trim()))
                return BadRequest(new { mensaje = "Estado inválido. Estados permitidos: Activo, Trasplantado, Fallecido" });

            if (!string.IsNullOrWhiteSpace(request.NivelUrgencia) && !NivelesUrgenciaValidos.Contains(request.NivelUrgencia.Trim()))
                return BadRequest(new { mensaje = "Nivel de urgencia inválido. Opciones: Alta, Media, Baja" });

            var estadoAnterior = paciente.Estado;
            var urgenciaAnterior = paciente.NivelUrgencia;

            if (!string.IsNullOrWhiteSpace(request.Estado))
                paciente.Estado = request.Estado.Trim();

            if (!string.IsNullOrWhiteSpace(request.NivelUrgencia))
                paciente.NivelUrgencia = request.NivelUrgencia.Trim();

            paciente.FechaActualizacion = DateTime.Now;

            try
            {
                await _context.SaveChangesAsync();

                try
                {
                    var cambios = "";
                    if (estadoAnterior != paciente.Estado)
                        cambios += $"Estado: {estadoAnterior} → {paciente.Estado}. ";
                    if (urgenciaAnterior != paciente.NivelUrgencia)
                        cambios += $"Urgencia: {urgenciaAnterior} → {paciente.NivelUrgencia}.";

                    await _bitacora.RegistrarAccionAsync(
                        usuarioId, "Actualizar Paciente", "Pacientes", paciente.Id,
                        $"Estado: {estadoAnterior}, Urgencia: {urgenciaAnterior}",
                        $"Estado: {paciente.Estado}, Urgencia: {paciente.NivelUrgencia}", cambios
                    );
                }
                catch { }

                await _context.Entry(paciente).Reference(p => p.Hospital).LoadAsync();

                return Ok(new
                {
                    mensaje = "Paciente actualizado correctamente",
                    paciente = new
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
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al actualizar el paciente", detalle = ex.Message });
            }
        }

        // GET: api/paciente/activos - Obtener conteo de pacientes activos
        [HttpGet("activos")]
        public async Task<ActionResult<object>> GetPacientesActivos()
        {
            try
            {
                var count = await _context.Pacientes
                    .Where(p => p.Estado == "Activo")
                    .CountAsync();

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
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

    public class UpdatePacienteRequest
    {
        public string? Estado { get; set; }
        public string? NivelUrgencia { get; set; }
    }

    public partial class UpdateEstadoPacienteRequest
    {
        public string NuevoEstado { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }
}