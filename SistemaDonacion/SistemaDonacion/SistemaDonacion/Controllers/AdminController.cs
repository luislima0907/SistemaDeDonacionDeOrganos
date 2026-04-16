using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Filters;
using System.Security.Claims;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Endpoint exclusivo para administradores
        /// </summary>
        [HttpGet("dashboard")]
        [AuthorizeRole("Administrador")]
        public IActionResult GetAdminDashboard()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "desconocido";
            _logger.LogInformation("[ADMIN] Acceso al dashboard | Usuario: {User}", userName);

            return Ok(new
            {
                message = "Acceso permitido a dashboard administrativo",
                usuario = userName,
                permisos = new[] { "Ver reportes", "Gestionar usuarios", "Confirmar asignaciones" }
            });
        }

        /// <summary>
        /// Endpoint para gestionar usuarios (solo admin)
        /// </summary>
        [HttpGet("usuarios")]
        [AuthorizeRole("Administrador")]
        public IActionResult GetUsuarios()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "desconocido";
            _logger.LogInformation("[ADMIN] Acceso a listado de usuarios | Usuario: {User}", userName);

            return Ok(new
            {
                message = "Listado de usuarios obtenido",
                usuarios = new object[] { }
            });
        }
    }
}