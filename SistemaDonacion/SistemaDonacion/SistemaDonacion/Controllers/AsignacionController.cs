using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using System.Security.Claims;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsignacionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IBitacoraService _bitacora;

        public AsignacionController(AppDbContext context, IBitacoraService bitacora)
        {
            _context = context;
            _bitacora = bitacora;
        }

        // POST: api/asignacion/confirmar
        [HttpPost("confirmar")]
        public async Task<IActionResult> ConfirmarAsignacion([FromBody] ConfirmarAsignacionRequest request)
        {
            if (request.OrganoId <= 0 || request.PacienteId <= 0)
                return BadRequest(new { mensaje = "Datos de asignación inválidos" });

            // Usar transacción para garantizar atomicidad (rollback si algo falla)
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Verificar que el órgano existe y está disponible
                var organo = await _context.Organos
                    .Include(o => o.Donante)
                    .FirstOrDefaultAsync(o => o.Id == request.OrganoId);

                if (organo == null)
                    return NotFound(new { mensaje = "Órgano no encontrado" });

                if (organo.Estado != "Disponible")
                    return BadRequest(new { mensaje = "El órgano no está disponible para asignación" });

                // Verificar que el paciente existe y está activo
                var paciente = await _context.Pacientes
                    .FirstOrDefaultAsync(p => p.Id == request.PacienteId);

                if (paciente == null)
                    return NotFound(new { mensaje = "Paciente no encontrado" });

                if (paciente.Estado != "Activo")
                    return BadRequest(new { mensaje = "El paciente no está activo" });

                // Cambiar estado del órgano a "Asignado"
                organo.Estado = "Asignado";
                organo.FechaActualizacion = DateTime.Now;

                // Cambiar estado del paciente a "En Trasplante"
                paciente.Estado = "En Trasplante";
                paciente.FechaActualizacion = DateTime.Now;

                await _context.SaveChangesAsync();

                // Registrar en bitácora
                var usuarioId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                await _bitacora.RegistrarAccionAsync(
                    usuarioId,
                    "Confirmar Asignación",
                    "Asignaciones",
                    organo.Id,
                    $"Órgano: {organo.TipoOrgano} | Donante: {organo.Donante?.Nombre}",
                    $"Paciente: {paciente.Nombre} | Estado órgano: Asignado | Estado paciente: En Trasplante",
                    request.Justificacion ?? "Asignación confirmada por administrador"
                );

                await transaction.CommitAsync();

                return Ok(new
                {
                    mensaje = "Asignación confirmada correctamente",
                    organo = new { organo.Id, organo.TipoOrgano, organo.Estado },
                    paciente = new { paciente.Id, paciente.Nombre, paciente.Estado }
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"[ERROR AsignacionController] {ex.Message}");
                return StatusCode(500, new { mensaje = "Error al confirmar la asignación, se realizó rollback" });
            }
        }
    }

    public class ConfirmarAsignacionRequest
    {
        public int OrganoId { get; set; }
        public int PacienteId { get; set; }
        public string? Justificacion { get; set; }
    }
}