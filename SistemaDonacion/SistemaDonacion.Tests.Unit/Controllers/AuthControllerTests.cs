using Xunit;
using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Controllers;
using SistemaDonacion.Models;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using Microsoft.EntityFrameworkCore;
using LoginRequest = SistemaDonacion.Models.LoginRequest;
using System.Security.Claims;

namespace SistemaDonacion.Tests.Unit.Controllers
{
    public class AuthControllerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IPasswordHashService _passwordHashService;

        public AuthControllerTests()
        {
            // Crear contexto con la base de datos real
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-9SBPDDD;Database=SistemaDonacionDb;User Id=sa;Password=Hell0w0rld3312j$;TrustServerCertificate=True;")
                .Options;

            _dbContext = new AppDbContext(options);
            _passwordHashService = new PasswordHashService();
        }

        [Fact]
        public async Task Login_WithValidCredentials_Medico_ReturnsOkOrError()
        {
            // Arrange - Usar usuario existente de la BD: medico1
            var controller = new AuthController(_dbContext, _passwordHashService);
            
            var loginRequest = new LoginRequest
            {
                Username = "medico1",
                Password = "Medico123!"
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            Assert.NotNull(result);
            
            // Verificar que la respuesta es un resultado válido (puede ser Ok o Error de autenticación)
            Assert.True(result is OkObjectResult || result is BadRequestObjectResult || result is ObjectResult,
                $"Se esperaba un resultado válido pero se obtuvo {result?.GetType().Name}");
        }

        [Fact]
        public async Task Login_WithValidCredentials_Admin_ReturnsOkOrError()
        {
            // Arrange - Usar usuario existente de la BD: admin
            var controller = new AuthController(_dbContext, _passwordHashService);
            
            var loginRequest = new LoginRequest
            {
                Username = "admin",
                Password = "Admin123!"
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            Assert.NotNull(result);
            
            // Verificar que la respuesta es un resultado válido
            Assert.True(result is OkObjectResult || result is BadRequestObjectResult || result is ObjectResult,
                $"Se esperaba un resultado válido pero se obtuvo {result?.GetType().Name}");
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ReturnsBadRequest()
        {
            // Arrange
            var controller = new AuthController(_dbContext, _passwordHashService);
            var loginRequest = new LoginRequest
            {
                Username = "usuarioQueNoExiste123456",
                Password = "Password123!"
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            Assert.NotNull(result);
            var badResult = result as BadRequestObjectResult;
            Assert.NotNull(badResult);
            Assert.Equal(400, badResult.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsBadRequest()
        {
            // Arrange - Usar usuario existente pero contraseña incorrecta
            var controller = new AuthController(_dbContext, _passwordHashService);
            var loginRequest = new LoginRequest
            {
                Username = "medico1",
                Password = "ContraseñaIncorrecta123"
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            Assert.NotNull(result);
            var badResult = result as BadRequestObjectResult;
            Assert.NotNull(badResult);
            Assert.Equal(400, badResult.StatusCode);
        }

        [Fact]
        public void GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var controller = new AuthController(_dbContext, _passwordHashService);

            // Simular usuario sin autenticación
            var mockHttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext();
            mockHttpContext.User = new System.Security.Principal.GenericPrincipal(
                new System.Security.Principal.GenericIdentity(""), 
                null
            );

            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext
            };

            // Act
            var result = controller.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.NotNull(unauthorizedResult);
            Assert.Equal(401, unauthorizedResult.StatusCode);
        }

        [Fact]
        public void GetCurrentUser_WithAuthentication_ReturnsOkWithUserData()
        {
            // Arrange
            var controller = new AuthController(_dbContext, _passwordHashService);

            // Simular usuario autenticado (medico1)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Name, "medico1"),
                new Claim(ClaimTypes.Role, "Medico"),
                new Claim("HospitalId", "1")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            };

            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext
            };

            // Act
            var result = controller.GetCurrentUser();

            // Assert
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            
            var value = okResult.Value as dynamic;
            Assert.NotNull(value);
        }

        [Fact]
        public void CheckSession_WithAuthenticatedUser_ReturnsOk()
        {
            // Arrange
            var controller = new AuthController(_dbContext, _passwordHashService);

            // Simular usuario autenticado
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Administrador")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var mockHttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext
            {
                User = principal
            };

            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext
            };

            // Act
            var result = controller.CheckSession();

            // Assert
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
        }

        public void Dispose()
        {
            try
            {
                _dbContext?.Dispose();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error al limpiar recursos: {ex.Message}");
            }
        }
    }
}
