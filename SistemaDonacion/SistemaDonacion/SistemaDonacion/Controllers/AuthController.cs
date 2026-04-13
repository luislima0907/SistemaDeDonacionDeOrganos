using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Models;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace SistemaDonacion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [IgnoreAntiforgeryToken]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHashService _passwordHashService;

        public AuthController(AppDbContext dbContext, IPasswordHashService passwordHashService)
        {
            _dbContext = dbContext;
            _passwordHashService = passwordHashService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Debe completar todos los campos" });

            try
            {
                // Buscar el usuario por nombre en la tabla Usuarios
                var user = await _dbContext.Usuarios
                    .FirstOrDefaultAsync(u => u.Nombre == request.Username);

                if (user == null)
                {
                    return BadRequest(new { message = "Credenciales inválidas" });
                }

                // Verificar si el usuario está activo
                if (!user.Estado)
                {
                    return Unauthorized(new { message = "Credenciales inválidas" });
                }

                // Verificar la contraseña usando el servicio de hash
                if (!_passwordHashService.VerifyPassword(request.Password!, user.Contrasenia))
                {
                    return BadRequest(new { message = "Credenciales inválidas" });
                }

                // Crear claims para la sesión (simulación de token)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Nombre),
                    new Claim(ClaimTypes.Role, user.Rol)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IssuedUtc = DateTimeOffset.UtcNow,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30),
                    IsPersistent = false
                };

                // Crear la cookie de autenticación
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return Ok(new { message = "OK", role = user.Rol, nombre = user.Nombre });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al intentar iniciar sesión: " + ex.Message });
            }
        }

        [HttpGet("current")]
        public IActionResult GetCurrentUser()
        {
            if (!User.Identity?.IsAuthenticated ?? false)
                return Unauthorized(new { message = "No hay usuario autenticado" });

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            return Ok(new { id = userId, nombre = userName, rol = role });
        }
    }
}
