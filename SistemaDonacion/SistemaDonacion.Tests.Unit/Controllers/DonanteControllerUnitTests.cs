using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SistemaDonacion.Controllers;
using SistemaDonacion.Data;
using SistemaDonacion.Models;
using SistemaDonacion.Services;
using Xunit;

namespace SistemaDonacion.Tests
{
    public class DonanteControllerUnitTests
    {
        private AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;

            return new AppDbContext(options);
        }

        private void ConfigurarContextoUsuario(
            DonanteController controller,
            string userId,
            string rol,
            string hospitalId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, rol),
                new Claim("HospitalId", hospitalId)
            };

            var identity = new ClaimsIdentity(claims, "Test");

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(identity)
                }
            };
        }

        [Fact]
        public async Task GetDonante_MedicoMismoHospital_RetornaOk()
        {
            // Arrange
            var context = CreateInMemoryContext("donante_test_db");

            var hospital = new Hospital
            {
                Id = 10,
                Nombre = "Hospital Central",
                Ciudad = "Guatemala",
                Estado = true
            };

            context.Hospitales.Add(hospital);

            var donante = new Donante
            {
                Id = 1,
                Nombre = "Juan Perez",
                TipoSanguineo = "O+",
                Edad = 30,
                Estado = "Disponible",
                HospitalId = 10,
                Hospital = hospital,
                Organos = new List<Organo>()
            };

            context.Donantes.Add(donante);

            await context.SaveChangesAsync();

            var mockBitacora = new Mock<IBitacoraService>();

            var controller = new DonanteController(
                context,
                mockBitacora.Object
            );

            ConfigurarContextoUsuario(
                controller,
                "2",
                "Medico",
                "10"
            );

            // Act
            var result = await controller.GetDonante(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);

            var okResult = Assert.IsType<OkObjectResult>(
                actionResult.Result
            );

            Assert.NotNull(okResult.Value);
        }
    }
}