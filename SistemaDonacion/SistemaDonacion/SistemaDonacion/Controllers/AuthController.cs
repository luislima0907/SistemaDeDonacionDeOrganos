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
                return BadRequest(new { message = "Credenciales inválidas" });

            try
            {
                // Buscar el usuario por nombre en la tabla Usuarios
                var user = await _dbContext.Usuarios
                    .FirstOrDefaultAsync(u => u.Nombre == request.Username);

                if (user == null || !user.Estado)
                {
                    return BadRequest(new { message = "Credenciales inválidas" });
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
                    new Claim(ClaimTypes.Role, user.Rol),
                    new Claim("HospitalId", user.HospitalId.HasValue
                        ? user.HospitalId.Value.ToString()
                        : "0")
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

                return Ok(new { message = "OK", role = user.Rol, nombre = user.Nombre, hospitalId = user.HospitalId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR LOGIN] {DateTime.UtcNow:O}");
                Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"InnerException: {ex.InnerException.Message}");
                    Console.WriteLine($"InnerException StackTrace: {ex.InnerException.StackTrace}");
                }
                return StatusCode(500, new { message = "Error al intentar iniciar sesión, intente nuevamente" });
            }
        }

        [HttpPost("logout")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "desconocido";

                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                Response.Cookies.Delete("SistemaDonacion.Auth", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict
                });

                Console.WriteLine($"[LOGOUT] {DateTime.UtcNow:O} | Usuario: {userName} | Sesión cerrada.");

                return Ok(new { message = "Sesión cerrada correctamente" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "No se pudo cerrar la sesión, intente nuevamente" });
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
            var hospitalId = User.FindFirst("HospitalId")?.Value;
            return Ok(new { id = userId, nombre = userName, rol = role, hospitalId = hospitalId });
        }

        [HttpGet("check-session")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult CheckSession()
        {
            return Ok(new { authenticated = true });
        }
    }

}
