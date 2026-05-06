using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Data;
using SistemaDonacion.Models;
using System.Security.Claims;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HospitalController : ControllerBase
    {
        private readonly AppDbContext _context;

        public HospitalController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/hospital - Obtener todos los hospitales activos
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Hospital>>> GetHospitales()
        {
            var hospitales = await _context.Hospitales
                .Where(h => h.Estado)
                .ToListAsync();
            return Ok(hospitales);
        }

        // GET: api/hospital/mi-hospital - Obtener solo el hospital del usuario autenticado
        [HttpGet("mi-hospital")]
        public async Task<ActionResult<object>> GetMiHospital()
        {
            try
            {
                var valor = User.FindFirst("HospitalId")?.Value;
                if (!int.TryParse(valor, out var hospitalId) || hospitalId <= 0)
                {
                    return Unauthorized(new { mensaje = "No tiene un hospital asignado. Contacte al administrador." });
                }

                var rol = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
                
                // Si es administrador, devolver null o un mensaje específico
                if (rol.Equals("Administrador", StringComparison.OrdinalIgnoreCase) ||
                    rol.Equals("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest(new { mensaje = "Los administradores deben seleccionar el hospital manualmente" });
                }

                var hospital = await _context.Hospitales
                    .Where(h => h.Id == hospitalId && h.Estado)
                    .FirstOrDefaultAsync();

                if (hospital == null)
                    return NotFound(new { mensaje = "Hospital no encontrado o inactivo" });

                return Ok(new
                {
                    hospital.Id,
                    hospital.Nombre,
                    hospital.Ciudad,
                    hospital.Pais,
                    hospital.Email,
                    hospital.Telefono
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { mensaje = "Error al obtener el hospital", error = ex.Message });
            }
        }

        // GET: api/hospital/{id} - Obtener un hospital específico
        [HttpGet("{id}")]
        public async Task<ActionResult<Hospital>> GetHospital(int id)
        {
            var hospital = await _context.Hospitales.FindAsync(id);

            if (hospital == null)
                return NotFound(new { mensaje = "Hospital no encontrado" });

            return Ok(hospital);
        }

        // POST: api/hospital - Registrar un nuevo hospital
        [HttpPost]
        public async Task<ActionResult<Hospital>> CrearHospital([FromBody] CreateHospitalRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Validar datos obligatorios
            if (string.IsNullOrWhiteSpace(request.Nombre) || 
                string.IsNullOrWhiteSpace(request.Ciudad) || 
                string.IsNullOrWhiteSpace(request.Pais))
                return BadRequest(new { mensaje = "Datos incompletos o inválidos" });

            // Verificar si el nombre ya existe
            var existe = await _context.Hospitales
                .AnyAsync(h => h.Nombre.ToLower() == request.Nombre.ToLower().Trim());
            
            if (existe)
                return BadRequest(new { mensaje = "Ya existe un hospital con ese nombre" });

            var hospital = new Hospital
            {
                Nombre = request.Nombre.Trim(),
                Ciudad = request.Ciudad.Trim(),
                Pais = request.Pais.Trim(),
                Telefono = request.Telefono?.Trim(),
                Email = request.Email?.Trim(),
                Estado = true,
                FechaRegistro = DateTime.Now
            };

            _context.Hospitales.Add(hospital);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetHospital), new { id = hospital.Id }, hospital);
        }

        // PUT: api/hospital/{id} - Actualizar información del hospital
        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarHospital(int id, [FromBody] UpdateHospitalRequest request)
        {
            var hospital = await _context.Hospitales.FindAsync(id);
            if (hospital == null)
                return NotFound(new { mensaje = "Hospital no encontrado" });

            // Validar que el nuevo nombre no esté en uso (por otro hospital)
            if (!string.IsNullOrWhiteSpace(request.Nombre) && 
                request.Nombre.Trim() != hospital.Nombre)
            {
                var existe = await _context.Hospitales
                    .AnyAsync(h => h.Nombre.ToLower() == request.Nombre.ToLower().Trim() && h.Id != id);
                
                if (existe)
                    return BadRequest(new { mensaje = "Ya existe otro hospital con ese nombre" });

                hospital.Nombre = request.Nombre.Trim();
            }

            if (!string.IsNullOrWhiteSpace(request.Ciudad))
                hospital.Ciudad = request.Ciudad.Trim();

            if (!string.IsNullOrWhiteSpace(request.Pais))
                hospital.Pais = request.Pais.Trim();

            if (!string.IsNullOrWhiteSpace(request.Telefono))
                hospital.Telefono = request.Telefono.Trim();

            if (!string.IsNullOrWhiteSpace(request.Email))
                hospital.Email = request.Email.Trim();

            _context.Hospitales.Update(hospital);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Hospital actualizado correctamente", hospital });
        }

        // PUT: api/hospital/{id}/estado - Cambiar estado del hospital
        [HttpPut("{id}/estado")]
        public async Task<IActionResult> CambiarEstadoHospital(int id, [FromBody] UpdateEstadoRequest request)
        {
            var hospital = await _context.Hospitales.FindAsync(id);
            if (hospital == null)
                return NotFound(new { mensaje = "Hospital no encontrado" });

            hospital.Estado = request.Activo;
            _context.Hospitales.Update(hospital);
            await _context.SaveChangesAsync();

            return Ok(new { mensaje = "Estado del hospital actualizado correctamente", hospital });
        }

        // GET: api/hospital/activos - Obtener conteo de hospitales activos
        [HttpGet("activos")]
        public async Task<ActionResult<object>> GetHospitalesActivos()
        {
            try
            {
                var count = await _context.Hospitales
                    .Where(h => h.Estado)
                    .CountAsync();

                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }

    public class CreateHospitalRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Ciudad { get; set; } = string.Empty;
        public string Pais { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }

    public class UpdateHospitalRequest
    {
        public string? Nombre { get; set; }
        public string? Ciudad { get; set; }
        public string? Pais { get; set; }
        public string? Telefono { get; set; }
        public string? Email { get; set; }
    }

    public partial class UpdateEstadoRequest
    {
        public bool Activo { get; set; }
    }
}
