using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Filters;
using System.Security.Claims;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class MedicoController : ControllerBase
    {
        private readonly ILogger<MedicoController> _logger;

        public MedicoController(ILogger<MedicoController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para acceso del panel médico (Médico y Administrador)
        /// </summary>
        [HttpGet("dashboard")]
        [AuthorizeRole("Medico", "Administrador")]
        public IActionResult GetMedicoDashboard()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "desconocido";
            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            _logger.LogInformation("[MEDICO] Acceso al dashboard | Usuario: {User} | Rol: {Role}", userName, role);

            return Ok(new
            {
                message = "Acceso permitido a panel médico",
                usuario = userName,
                rol = role,
                permisos = new[] { "Registrar pacientes", "Registrar donantes", "Ver asignaciones" }
            });
        }
    }
}
