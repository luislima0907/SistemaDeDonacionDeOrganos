using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Services;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class HelperController : ControllerBase
    {
        private readonly IPasswordHashService _passwordHashService;

        public HelperController(IPasswordHashService passwordHashService)
        {
            _passwordHashService = passwordHashService;
        }

        /// <summary>
        /// Genera un hash de contraseña para testing.
        /// Ejemplo: GET /api/helper/generate-hash?password=Admin123!
        /// </summary>
        [HttpGet("generate-hash")]
        public IActionResult GenerateHash(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return BadRequest(new { message = "Contraseña requerida" });

            var hash = _passwordHashService.HashPassword(password);
            return Ok(new 
            { 
                password = password,
                hash = hash,
                instructions = $"UPDATE dbo.Usuarios SET Contrasenia = '{hash}' WHERE Nombre = 'admin';"
            });
        }
    }
}

