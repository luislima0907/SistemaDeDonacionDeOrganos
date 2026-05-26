using Xunit;
using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Controllers;
using SistemaDonacion.Models;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace SistemaDonacion.Tests.Unit.Controllers
{
    public class AdminControllerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;

        public AdminControllerTests()
        {
            // Crear contexto con la base de datos real
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer("Server=DESKTOP-9SBPDDD;Database=SistemaDonacionDb;User Id=sa;Password=Hell0w0rld3312j$;TrustServerCertificate=True;")
                .Options;

            _dbContext = new AppDbContext(options);
        }

        [Fact]
        public void GetAdminDashboard_WithAdminRole_ReturnsOk()
        {
            // Arrange
            var logger = new MockLoggerAdmin<AdminController>();
            var controller = new AdminController(logger);

            // Simular usuario administrador autenticado (admin de la BD)
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
            var result = controller.GetAdminDashboard();

            // Assert
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            
            var value = okResult.Value as dynamic;
            Assert.NotNull(value);
        }

        [Fact]
        public void GetUsuarios_WithAdminRole_ReturnsOk()
        {
            // Arrange
            var logger = new MockLoggerAdmin<AdminController>();
            var controller = new AdminController(logger);

            // Simular usuario administrador autenticado (admin de la BD)
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
            var result = controller.GetUsuarios();

            // Assert
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            
            var value = okResult.Value as dynamic;
            Assert.NotNull(value);
        }

        [Fact]
        public void GetAdminDashboard_ReturnsMessageAndPermissions()
        {
            // Arrange
            var logger = new MockLoggerAdmin<AdminController>();
            var controller = new AdminController(logger);

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
            var result = controller.GetAdminDashboard();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            
            var value = okResult.Value as dynamic;
            Assert.NotNull(value);
        }

        [Fact]
        public void GetUsuarios_ReturnsListMessage()
        {
            // Arrange
            var logger = new MockLoggerAdmin<AdminController>();
            var controller = new AdminController(logger);

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
            var result = controller.GetUsuarios();

            // Assert
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            
            var value = okResult.Value as dynamic;
            Assert.NotNull(value);
        }

        [Fact]
        public void GetAdminDashboard_WithAdmin2_ReturnsOk()
        {
            // Arrange - Usar otro usuario administrador (admin2)
            var logger = new MockLoggerAdmin<AdminController>();
            var controller = new AdminController(logger);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "4"),
                new Claim(ClaimTypes.Name, "admin2"),
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
            var result = controller.GetAdminDashboard();

            // Assert
            Assert.NotNull(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);
            Assert.Equal(200, okResult.StatusCode);
            
            var value = okResult.Value as dynamic;
            Assert.NotNull(value);
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

    // Mock Logger para pruebas
    public class MockLoggerAdmin<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable BeginScope<TState>(TState state) => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) { }
    }
}
