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
 
        private int? ObtenerHospitalIdDelUsuario()
        {
            var valor = User.FindFirst("HospitalId")?.Value;
            if (int.TryParse(valor, out var id) && id > 0)
                return id;
            return null;
        }
 
        private async Task RMh4hfsi84LdbS4uS3jaSaNccc8kartkDJ(int usuarioId, int registroId, int hospitalMedico, int hospitalRegistro)
        {
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId, "ACCESO_DENEGADO_HOSPITAL_CRUZADO", "Donantes",
                    registroId, $"HospitalId del médico: {hospitalMedico}",
                    $"HospitalId del registro: {hospitalRegistro}",
                    "Intento de acceso cruzado entre hospitales detectado y bloqueado."
                );
            }
            catch { }
        }
 
        // GET: api/donante
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetDonantes()
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            IQueryable<Donante> query = _context.Donantes.Include(d => d.Hospital);
 
            if (!rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                !rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var hospitalId = ObtenerHospitalIdDelUsuario();
                if (hospitalId == null)
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });
                query = query.Where(d => d.HospitalId == hospitalId.Value);
            }
 
            var donantes = await query.ToListAsync();
 
            var resultado = donantes.Select(d => new
            {
                d.Id, d.Nombre, d.TipoSanguineo, d.Edad,
                d.FechaRegistro, d.Estado, d.HospitalId,
                d.Observaciones, d.FechaActualizacion,
                Hospital = d.Hospital != null ? new { d.Hospital.Id, d.Hospital.Nombre, d.Hospital.Ciudad } : null,
                Organos = d.Organos.Select(o => new { o.Id, o.TipoOrgano, o.Estado, o.FechaDisponibilidad })
            }).ToList();
 
            return Ok(resultado);
        }
 
        // GET: api/donante/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetDonante(int id)
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
 
            var donante = await _context.Donantes
                .Include(d => d.Hospital)
                .Include(d => d.Organos)
                .FirstOrDefaultAsync(d => d.Id == id);
 
            if (donante == null)
                return NotFound(new { mensaje = "Donante no encontrado" });
 
            // Médico solo puede ver donantes de su hospital
            if (!rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                !rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var hospitalId = ObtenerHospitalIdDelUsuario();
                if (hospitalId == null)
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });
 
                if (donante.HospitalId != hospitalId.Value)
                {
                    await RMh4hfsi84LdbS4uS3jaSaNccc8kartkDJ(usuarioId, donante.Id, hospitalId.Value, donante.HospitalId);
                    return StatusCode(403, new { mensaje = "No tiene permisos para acceder a este donante" });
                }
            }
 
            return Ok(new
            {
                donante.Id, donante.Nombre, donante.TipoSanguineo, donante.Edad,
                donante.FechaRegistro, donante.Estado, donante.HospitalId,
                donante.Observaciones, donante.FechaActualizacion,
                Hospital = donante.Hospital != null ? new { donante.Hospital.Id, donante.Hospital.Nombre, donante.Hospital.Ciudad } : null,
                Organos = donante.Organos.Select(o => new { o.Id, o.TipoOrgano, o.Estado, o.FechaDisponibilidad })
            });
        }
 
        // POST: api/donante
        [HttpPost]
        public async Task<ActionResult<object>> CrearDonante([FromBody] CreateDonanteRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
 
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (usuarioId == 0)
                return Unauthorized(new { mensaje = "Usuario no autenticado" });
 
            if (string.IsNullOrWhiteSpace(request.Nombre) ||
                string.IsNullOrWhiteSpace(request.TipoSanguineo) ||
                request.Edad <= 0)
                return BadRequest(new { mensaje = "Datos incompletos o inválidos" });
 
            // Determinar hospitalId según rol
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            int hospitalIdFinal;
 
            if (rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                hospitalIdFinal = request.HospitalId;
            }
            else
            {
                var hospitalIdMedico = ObtenerHospitalIdDelUsuario();
                if (hospitalIdMedico == null)
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });
 
                if (request.HospitalId != 0 && request.HospitalId != hospitalIdMedico.Value)
                {
                    await RMh4hfsi84LdbS4uS3jaSaNccc8kartkDJ(usuarioId, 0, hospitalIdMedico.Value, request.HospitalId);
                    return StatusCode(403, new { mensaje = "No tiene permisos para registrar donantes en otro hospital" });
                }
                hospitalIdFinal = hospitalIdMedico.Value;
            }
 
            var hospitalExiste = await _context.Hospitales.AnyAsync(h => h.Id == hospitalIdFinal && h.Estado);
            if (!hospitalExiste)
                return BadRequest(new { mensaje = "Hospital inválido o inactivo" });
 
            var donante = new Donante
            {
                Nombre = request.Nombre.Trim(),
                TipoSanguineo = request.TipoSanguineo.ToUpper().Trim(),
                Edad = request.Edad,
                HospitalId = hospitalIdFinal,
                Estado = "Disponible",
                Observaciones = request.Observaciones?.Trim(),
                FechaRegistro = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };
 
            _context.Donantes.Add(donante);
            await _context.SaveChangesAsync();
 
            await _context.Entry(donante).Reference(d => d.Hospital).LoadAsync();
            await _context.Entry(donante).Collection(d => d.Organos).LoadAsync();
 
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId, "Registrar Donante", "Donantes", donante.Id,
                    null, $"Donante: {donante.Nombre}, Tipo Sanguíneo: {donante.TipoSanguineo}",
                    "Nuevo donante registrado en el sistema"
                );
            }
            catch { }
 
            return CreatedAtAction(nameof(GetDonante), new { id = donante.Id }, new
            {
                donante.Id, donante.Nombre, donante.TipoSanguineo, donante.Edad,
                donante.FechaRegistro, donante.Estado, donante.HospitalId,
                donante.Observaciones, donante.FechaActualizacion,
                Hospital = donante.Hospital != null ? new { donante.Hospital.Id, donante.Hospital.Nombre, donante.Hospital.Ciudad } : null,
                Organos = donante.Organos.Select(o => new { o.Id, o.TipoOrgano, o.Estado, o.FechaDisponibilidad })
            });
        }
 
        // PUT: api/donante/{id}/estado
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> ActualizarEstadoDonante(int id, [FromBody] UpdateEstadoRequest request)
        {
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
 
            var donante = await _context.Donantes.FindAsync(id);
            if (donante == null)
                return NotFound(new { mensaje = "Donante no encontrado" });
 
            // Médico solo puede modificar donantes de su hospital
            if (!rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                !rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var hospitalId = ObtenerHospitalIdDelUsuario();
                if (hospitalId == null)
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });
 
                if (donante.HospitalId != hospitalId.Value)
                {
                    await RMh4hfsi84LdbS4uS3jaSaNccc8kartkDJ(usuarioId, donante.Id, hospitalId.Value, donante.HospitalId);
                    return StatusCode(403, new { mensaje = "No tiene permisos para modificar este donante" });
                }
            }
 
            var estadosValidos = new[] { "Disponible", "Asignado", "Rechazado" };
            if (!estadosValidos.Contains(request.NuevoEstado))
                return BadRequest(new { mensaje = "Estado inválido" });
 
            var estadoAnterior = donante.Estado;
            donante.Estado = request.NuevoEstado;
            donante.FechaActualizacion = DateTime.Now;
 
            _context.Donantes.Update(donante);
            await _context.SaveChangesAsync();
 
            try
            {
                await _bitacora.RegistrarAccionAsync(
                    usuarioId, "Actualizar Estado Donante", "Donantes", donante.Id,
                    $"Estado anterior: {estadoAnterior}",
                    $"Nuevo estado: {donante.Estado}",
                    request.Observaciones?.Trim()
                );
            }
            catch { }
 
            return Ok(new { mensaje = "Estado actualizado correctamente", donante });
        }
 
        // GET: api/donante/{donanteId}/organos
        [HttpGet("{donanteId}/organos")]
        public async Task<ActionResult<IEnumerable<Organo>>> GetOrganosPorDonante(int donanteId)
        {
            var donante = await _context.Donantes.FindAsync(donanteId);
            if (donante == null)
                return NotFound(new { mensaje = "Donante no encontrado" });
 
            // Validar acceso por hospital
            var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
                !rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                var hospitalId = ObtenerHospitalIdDelUsuario();
                if (hospitalId == null)
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado." });
 
                if (donante.HospitalId != hospitalId.Value)
                    return StatusCode(403, new { mensaje = "No tiene permisos para ver órganos de este donante" });
            }
 
            var organos = await _context.Organos
                .Where(o => o.DonanteId == donanteId)
                .ToListAsync();
 
            return Ok(organos);
        }
 
        // GET: api/donante/activos
        [HttpGet("activos")]
        public async Task<ActionResult<object>> GetDonantesActivos()
        {
            try
            {
                var count = await _context.Donantes
                    .Where(d => d.Estado == "Disponible")
                    .CountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
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
 