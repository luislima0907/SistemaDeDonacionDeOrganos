using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using SistemaDonacion.Controllers;
using SistemaDonacion.Models;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using Microsoft.EntityFrameworkCore;
using LoginRequest = SistemaDonacion.Models.LoginRequest;

namespace SistemaDonacion.Tests.Unit.Controllers
{
    public class AuthControllerTests
    {
        private AppDbContext CreateTestDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;

            var context = new AppDbContext(options);
            
            context.Usuarios.AddRange(
                new ApplicationUser
                {
                    Id = 1,
                    Nombre = "medico1",
                    Contrasenia = "$PBKDF2$10000$PBPYvv07oE+ZTjggclVYmA==$nzOtI1jl67AjxOGYaRjweYFxLX6slRPP1zBRc60kw8A=",
                    Estado = true,
                    Rol = "Medico",
                    HospitalId = 1
                },
                new ApplicationUser
                {
                    Id = 2,
                    Nombre = "inactiveuser",
                    Contrasenia = "$PBKDF2$10000$PBPYvv07oE+ZTjggclVYmA==$nzOtI1jl67AjxOGYaRjweYFxLX6slRPP1zBRc60kw8A=",
                    Estado = false,
                    Rol = "Medico",
                    HospitalId = 1
                }
            );
            context.SaveChanges();
            return context;
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ReturnsBadRequest()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            var mockPasswordService = new Mock<IPasswordHashService>();

            var controller = new AuthController(dbContext, mockPasswordService.Object);
            var loginRequest = new LoginRequest 
            { 
                Username = "usuarioInexistente", 
                Password = "Password123!" 
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(400, objectResult?.StatusCode);
        }

        [Fact]
        public async Task Login_WithInactiveUser_ReturnsBadRequest()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            var mockPasswordService = new Mock<IPasswordHashService>();
            mockPasswordService
                .Setup(p => p.VerifyPassword("Password123!", 
                    "$PBKDF2$10000$PBPYvv07oE+ZTjggclVYmA==$nzOtI1jl67AjxOGYaRjweYFxLX6slRPP1zBRc60kw8A="))
                .Returns(true);

            var controller = new AuthController(dbContext, mockPasswordService.Object);
            var loginRequest = new LoginRequest 
            { 
                Username = "inactiveuser", 
                Password = "Password123!" 
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(400, objectResult?.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsBadRequest()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            var mockPasswordService = new Mock<IPasswordHashService>();
            mockPasswordService
                .Setup(p => p.VerifyPassword("PasswordIncorrecto", 
                    "$PBKDF2$10000$PBPYvv07oE+ZTjggclVYmA==$nzOtI1jl67AjxOGYaRjweYFxLX6slRPP1zBRc60kw8A="))
                .Returns(false);

            var controller = new AuthController(dbContext, mockPasswordService.Object);
            var loginRequest = new LoginRequest 
            { 
                Username = "medico1", 
                Password = "PasswordIncorrecto" 
            };

            // Act
            var result = await controller.Login(loginRequest);

            // Assert
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(400, objectResult?.StatusCode);
        }

        [Fact]
        public void GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Arrange
            var dbContext = CreateTestDbContext();
            var mockPasswordService = new Mock<IPasswordHashService>();
            var controller = new AuthController(dbContext, mockPasswordService.Object);
            
            // Crear un HttpContext mockeado con un User sin autenticación
            var mockHttpContext = new Mock<Microsoft.AspNetCore.Http.HttpContext>();
            var identity = new System.Security.Principal.GenericIdentity("");
            var principal = new System.Security.Principal.GenericPrincipal(identity, null);
            mockHttpContext.Setup(x => x.User).Returns(principal);
            
            controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext
            {
                HttpContext = mockHttpContext.Object
            };

            // Act
            var result = controller.GetCurrentUser();

            // Assert
            var objectResult = result as ObjectResult;
            Assert.NotNull(objectResult);
            Assert.Equal(401, objectResult?.StatusCode);
        }
    }
}
