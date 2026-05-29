using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SistemaDonacion.Controllers;
using SistemaDonacion.Data;
using SistemaDonacion.Models;
using SistemaDonacion.Services;
using System.Security.Claims;
using Xunit;

namespace SistemaDonacion.Tests.Unit.Controllers
{
    public class RankingControllerTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly IRankingService _rankingService;
        private readonly Mock<IBitacoraService> _bitacoraMock;

        public RankingControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _rankingService = new RankingService(_dbContext);
            _bitacoraMock = new Mock<IBitacoraService>();

            CargarDatosPrueba();
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConDatosValidos_RetornaOk()
        {
            // Arrange
            var controller = CrearControllerAutenticado();

            // Act
            var result = await controller.ObtenerRankingPorTipo("Riñón", "O+");

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConTipoOrganoVacio_RetornaBadRequest()
        {
            // Arrange
            var controller = CrearControllerAutenticado();

            // Act
            var result = await controller.ObtenerRankingPorTipo("", "O+");

            // Assert
            Assert.NotNull(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConTipoSanguineoVacio_RetornaBadRequest()
        {
            // Arrange
            var controller = CrearControllerAutenticado();

            // Act
            var result = await controller.ObtenerRankingPorTipo("Riñón", "");

            // Assert
            Assert.NotNull(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_SinPacientesCompatibles_RetornaOkConMensaje()
        {
            // Arrange
            var controller = CrearControllerAutenticado();

            // Act
            var result = await controller.ObtenerRankingPorTipo("Corazón", "AB-");

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_ConOrganoDisponible_RetornaOk()
        {
            // Arrange
            var controller = CrearControllerAutenticado();

            // Act
            var result = await controller.ObtenerRankingPrioridad(1);

            // Assert
            Assert.NotNull(result);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_ConOrganoInexistente_RetornaNotFound()
        {
            // Arrange
            var controller = CrearControllerAutenticado();

            // Act
            var result = await controller.ObtenerRankingPrioridad(999);

            // Assert
            Assert.NotNull(result);
            var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_ConOrganoNoDisponible_RetornaBadRequest()
        {
            // Arrange
            var controller = CrearControllerAutenticado();

            // Act
            var result = await controller.ObtenerRankingPrioridad(2);

            // Assert
            Assert.NotNull(result);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task RankingService_OrdenaPacientesPorPuntajeDescendente()
        {
            // Act
            var ranking = await _rankingService.ObtenerRankingPrioridadPorTipoOrganoAsync("Riñón", "O+");

            // Assert
            Assert.NotNull(ranking);
            Assert.True(ranking.Count >= 2);
            Assert.True(ranking[0].PuntajeTotal >= ranking[1].PuntajeTotal);
            Assert.Equal(1, ranking[0].Posicion);
            Assert.Equal(2, ranking[1].Posicion);
        }

        [Fact]
        public async Task RankingService_FiltraPacientesActivosYCompatibles()
        {
            // Act
            var ranking = await _rankingService.ObtenerRankingPrioridadPorTipoOrganoAsync("Riñón", "O+");

            // Assert
            Assert.NotNull(ranking);
            Assert.All(ranking, item =>
            {
                Assert.Equal("Riñón", item.OrganoRequerido);
                Assert.Equal("Activo", item.Estado);
                Assert.True(item.CompatibilidadVerificada);
            });
        }

        private OrganoController CrearControllerAutenticado()
        {
            var controller = new OrganoController(_dbContext, _bitacoraMock.Object, _rankingService);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "Administrador"),
                new Claim("HospitalId", "1")
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };

            return controller;
        }

        private void CargarDatosPrueba()
        {
            var hospital = new Hospital
            {
                Id = 1,
                Nombre = "Hospital de Prueba",
                Ciudad = "Jalapa",
                Pais = "Guatemala",
                Telefono = "12345678",
                Email = "hospital@prueba.com",
                Estado = true
            };

            var donante = new Donante
            {
                Id = 1,
                Nombre = "Donante Prueba",
                TipoSanguineo = "O+",
                Edad = 35,
                HospitalId = 1,
                Estado = "Disponible",
                FechaRegistro = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            var organoDisponible = new Organo
            {
                Id = 1,
                DonanteId = 1,
                TipoOrgano = "Riñón",
                Estado = "Disponible",
                FechaDisponibilidad = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            var organoNoDisponible = new Organo
            {
                Id = 2,
                DonanteId = 1,
                TipoOrgano = "Riñón",
                Estado = "Asignado",
                FechaDisponibilidad = DateTime.Now,
                FechaActualizacion = DateTime.Now
            };

            var pacientes = new List<Paciente>
            {
                new Paciente
                {
                    Id = 1,
                    Nombre = "Paciente Alta",
                    TipoSanguineo = "O+",
                    OrganoRequerido = "Riñón",
                    NivelUrgencia = "Alta",
                    HospitalId = 1,
                    Estado = "Activo",
                    FechaRegistro = DateTime.Now.AddDays(-5),
                    FechaActualizacion = DateTime.Now
                },
                new Paciente
                {
                    Id = 2,
                    Nombre = "Paciente Media",
                    TipoSanguineo = "O+",
                    OrganoRequerido = "Riñón",
                    NivelUrgencia = "Media",
                    HospitalId = 1,
                    Estado = "Activo",
                    FechaRegistro = DateTime.Now.AddDays(-3),
                    FechaActualizacion = DateTime.Now
                },
                new Paciente
                {
                    Id = 3,
                    Nombre = "Paciente Inactivo",
                    TipoSanguineo = "O+",
                    OrganoRequerido = "Riñón",
                    NivelUrgencia = "Alta",
                    HospitalId = 1,
                    Estado = "Inactivo",
                    FechaRegistro = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                },
                new Paciente
                {
                    Id = 4,
                    Nombre = "Paciente Otro Organo",
                    TipoSanguineo = "O+",
                    OrganoRequerido = "Corazón",
                    NivelUrgencia = "Alta",
                    HospitalId = 1,
                    Estado = "Activo",
                    FechaRegistro = DateTime.Now,
                    FechaActualizacion = DateTime.Now
                }
            };

            _dbContext.Hospitales.Add(hospital);
            _dbContext.Donantes.Add(donante);
            _dbContext.Organos.AddRange(organoDisponible, organoNoDisponible);
            _dbContext.Pacientes.AddRange(pacientes);
            _dbContext.SaveChanges();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }
    }
}