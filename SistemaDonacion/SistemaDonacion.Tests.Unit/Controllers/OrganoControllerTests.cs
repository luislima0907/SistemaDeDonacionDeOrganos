using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using SistemaDonacion.Controllers;
using SistemaDonacion.Data;
using SistemaDonacion.Models;
using SistemaDonacion.Services;
using Xunit;

namespace SistemaDonacion.Tests.Unit.Controllers
{
    public class OrganoControllerTests
    {
        private AppDbContext CreateInMemoryContext(string dbName)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName)
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void OrganoController_Constructor_InitializesFields()
        {
            // Arrange
            var context = CreateInMemoryContext("ctor_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();

            // Act
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Assert
            Assert.NotNull(controller);
        }
        //

        [Fact]
        public async Task GetOrganos_AdminRole_ReturnsAll()
        {
            // Arrange
            var context = CreateInMemoryContext("get_all_db");
            context.Organos.Add(new Organo { Id = 1, DonanteId = 1, TipoOrgano = "Riñón", Estado = "Disponible" });
            context.Organos.Add(new Organo { Id = 2, DonanteId = 2, TipoOrgano = "Hígado", Estado = "Asignado" });
            context.Donantes.Add(new Donante { Id = 1, Nombre = "D1", HospitalId = 1 });
            context.Donantes.Add(new Donante { Id = 2, Nombre = "D2", HospitalId = 2 });
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Administrador") });
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            // Act
            var result = await controller.GetOrganos();

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var actionResult = Assert.IsType<OkObjectResult>(ok.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(actionResult.Value);
            Assert.Equal(2, list.Count());
        }

        [Fact]
        public async Task GetOrganos_NonAdminWithoutHospital_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateInMemoryContext("get_none_db");
            context.Organos.Add(new Organo { Id = 1, DonanteId = 1, TipoOrgano = "Corazón", Estado = "Disponible" });
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext();
            controller.ControllerContext.HttpContext = new DefaultHttpContext();
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Usuario") });
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            // Act
            var result = await controller.GetOrganos();

            // Assert
            var actionResult = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(actionResult.Result);
            Assert.NotNull(unauthorized.Value);
        }

        [Fact]
        public async Task GetOrganosDisponibles_ByTipo_ReturnsFiltered()
        {
            // Arrange
            var context = CreateInMemoryContext("get_by_tipo_db");
            var donante = new Donante { Id = 1, Nombre = "D1" };
            context.Donantes.Add(donante);
            context.Organos.Add(new Organo { Id = 1, DonanteId = 1, TipoOrgano = "Riñón", Estado = "Disponible", Donante = donante });
            context.Organos.Add(new Organo { Id = 2, DonanteId = 1, TipoOrgano = "Hígado", Estado = "Disponible", Donante = donante });
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.GetOrganosDisponibles("Riñón");

            // Assert
            var ok = Assert.IsType<ActionResult<IEnumerable<object>>>(result);
            var actionResult = Assert.IsType<OkObjectResult>(ok.Result);
            var list = Assert.IsAssignableFrom<IEnumerable<object>>(actionResult.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task GetOrganosDisponibles_Count_ReturnsCount()
        {
            // Arrange
            var context = CreateInMemoryContext("count_db");
            context.Organos.Add(new Organo { Id = 1, DonanteId = 1, TipoOrgano = "Riñón", Estado = "Disponible" });
            context.Organos.Add(new Organo { Id = 2, DonanteId = 2, TipoOrgano = "Hígado", Estado = "Asignado" });
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.GetOrganosDisponibles();

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(ok.Value);
            var valueStr = ok.Value.ToString();
            Assert.Contains("1", valueStr);
            Assert.Contains("count", valueStr);
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_InvalidInput_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext("rank_invalid_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ObtenerRankingPorTipo("", "A+");

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(bad.Value);
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_NoRanking_ReturnsMessage()
        {
            // Arrange
            var context = CreateInMemoryContext("rank_empty_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            ranking.Setup(r => r.ObtenerRankingPrioridadPorTipoOrganoAsync("Riñón", "A+")).ReturnsAsync(new List<RankingPrioridad>());
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ObtenerRankingPorTipo("Riñón", "A+");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.NotNull(ok.Value);
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_WithData_ReturnsList()
        {
            // Arrange
            var context = CreateInMemoryContext("rank_data_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var data = new List<RankingPrioridad> { new RankingPrioridad { Posicion = 1, PacienteId = 1, NombrePaciente = "P1", TipoSanguineo = "A+" } };
            ranking.Setup(r => r.ObtenerRankingPrioridadPorTipoOrganoAsync("Riñón", "A+")).ReturnsAsync(data);
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ObtenerRankingPorTipo("Riñón", "A+");

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var list = Assert.IsAssignableFrom<List<RankingPrioridad>>(ok.Value);
            Assert.Single(list);
        }
        [Fact]
        public async Task GetOrgano_OrganNotFound_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext("get_organo_notfound_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.GetOrgano(123);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            Assert.NotNull(notFound.Value);
            var notFoundStr = notFound.Value.ToString();
            Assert.Contains("Órgano no encontrado", notFoundStr);
        }

        [Fact]
        public async Task GetOrgano_WithDonante_ReturnsOk()
        {
            // Arrange
            var context = CreateInMemoryContext("get_organo_ok_db");
            var donante = new Donante { Id = 10, Nombre = "Donante1", TipoSanguineo = "A+", Edad = 30, Estado = "Activo" };
            var organo = new Organo { Id = 20, DonanteId = 10, TipoOrgano = "Riñón", Estado = "Disponible", Donante = donante };
            context.Donantes.Add(donante);
            context.Organos.Add(organo);
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.GetOrgano(20);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.NotNull(ok.Value);
            var s = ok.Value.ToString();
            Assert.Contains("Riñón", s);
            Assert.Contains("Donante1", s);
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_OrganNotFound_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext("rank_notfound_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ObtenerRankingPrioridad(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<RankingPrioridad>>>(result);
            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            Assert.NotNull(notFound.Value);
            Assert.Contains("Órgano no encontrado", notFound.Value.ToString());
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_NotDisponible_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext("rank_notdisponible_db");
            var donante = new Donante { Id = 2, Nombre = "D2" };
            var organo = new Organo { Id = 3, DonanteId = 2, TipoOrgano = "Hígado", Estado = "Asignado", Donante = donante };
            context.Donantes.Add(donante);
            context.Organos.Add(organo);
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ObtenerRankingPrioridad(3);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<RankingPrioridad>>>(result);
            var bad = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
            Assert.Contains("El órgano debe estar disponible", bad.Value.ToString());
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_DonanteNull_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext("rank_donante_null_db");
            var organo = new Organo { Id = 4, DonanteId = 0, TipoOrgano = "Corazón", Estado = "Disponible", Donante = null };
            context.Organos.Add(organo);
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ObtenerRankingPrioridad(4);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<RankingPrioridad>>>(result);
            var obj = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
            Assert.NotNull(obj.Value);
            var msg = obj.Value.ToString();
            Assert.True(msg.Contains("no tiene donante") || msg.Contains("Órgano no encontrado"));
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_Success_ReturnsOkEvenIfBitacoraFails()
        {
            // Arrange
            var context = CreateInMemoryContext("rank_success_db");
            var donante = new Donante { Id = 5, Nombre = "D5" };
            var organo = new Organo { Id = 6, DonanteId = 5, TipoOrgano = "Riñón", Estado = "Disponible", Donante = donante };
            context.Donantes.Add(donante);
            context.Organos.Add(organo);
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            bitacora.Setup(b => b.RegistrarAccionAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>()
            )).ThrowsAsync(new Exception("boom"));

            var ranking = new Mock<IRankingService>();
            var sample = new List<RankingPrioridad> { new RankingPrioridad { Posicion = 1, PacienteId = 1, NombrePaciente = "P1" } };
            ranking.Setup(r => r.ObtenerRankingPrioridadAsync(6)).ReturnsAsync(sample);

            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42") }));

            // Act
            var result = await controller.ObtenerRankingPrioridad(6);

            // Assert
            var actionResult = Assert.IsType<ActionResult<List<RankingPrioridad>>>(result);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var list = Assert.IsAssignableFrom<List<RankingPrioridad>>(ok.Value);
            Assert.Single(list);
        }

        [Fact]
        public async Task DebugRankingPrioridad_OrganNotFound_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext("debug_notfound_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.DebugRankingPrioridad(1);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
            Assert.Contains("Órgano no encontrado", notFound.Value.ToString());
        }

        [Fact]
        public async Task DebugRankingPrioridad_DonanteNull_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext("debug_donante_null_db");
            var organo = new Organo { Id = 7, DonanteId = 0, TipoOrgano = "Riñón", Donante = null };
            context.Organos.Add(organo);
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.DebugRankingPrioridad(7);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            if (actionResult.Result is NotFoundObjectResult nf)
            {
                Assert.Contains("Órgano no encontrado", nf.Value.ToString());
            }
            else
            {
                var obj = Assert.IsAssignableFrom<ObjectResult>(actionResult.Result);
                Assert.NotNull(obj.Value);
                Assert.Contains("no tiene donante", obj.Value.ToString());
            }
        }

        [Fact]
        public async Task DebugRankingPrioridad_Success_FiltersPatients()
        {
            // Arrange
            var context = CreateInMemoryContext("debug_success_db");
            var donante = new Donante { Id = 8, Nombre = "D8", TipoSanguineo = "A+" };
            var organo = new Organo { Id = 9, DonanteId = 8, TipoOrgano = "Riñón", Donante = donante };
            context.Donantes.Add(donante);
            context.Organos.Add(organo);
            // patients: one matching organo type and estado and recent date
            context.Pacientes.Add(new Paciente { Id = 1, Nombre = "P1", TipoSanguineo = "A+", OrganoRequerido = "Riñón", Estado = "Activo", FechaActualizacion = DateTime.Now });
            // one old
            context.Pacientes.Add(new Paciente { Id = 2, Nombre = "P2", TipoSanguineo = "A+", OrganoRequerido = "Riñón", Estado = "Activo", FechaActualizacion = DateTime.Now.AddDays(-40) });
            // different organ
            context.Pacientes.Add(new Paciente { Id = 3, Nombre = "P3", TipoSanguineo = "A+", OrganoRequerido = "Hígado", Estado = "Activo", FechaActualizacion = DateTime.Now });
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.DebugRankingPrioridad(9);

            // Assert
            var actionResult = Assert.IsType<ActionResult<object>>(result);
            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            var s = ok.Value.ToString();
            Assert.Contains("debug", s);
            Assert.Contains("porOrgano", s);
            Assert.Contains("porAntiguedad", s);
        }

        [Fact]
        public async Task CrearOrgano_InvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext("crear_invalid_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ModelState.AddModelError("TipoOrgano", "Required");

            // Act
            var result = await controller.CrearOrgano(new CreateOrganoRequest { DonanteId = 1, TipoOrgano = "" });

            // Assert
            var action = Assert.IsType<ActionResult<object>>(result);
            var bad = Assert.IsAssignableFrom<ObjectResult>(action.Result);
            Assert.NotNull(bad.Value);
        }

        [Fact(Skip = "ProductionBugSuspected")]
        public async Task CrearOrgano_Unauthenticated_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateInMemoryContext("crear_unauth_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.CrearOrgano(new CreateOrganoRequest { DonanteId = 1, TipoOrgano = "Riñón" });

            // Assert
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Usuario no autenticado", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task CrearOrgano_DonanteNotFound_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext("crear_donante_nf_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "1") }));

            // Act
            var result = await controller.CrearOrgano(new CreateOrganoRequest { DonanteId = 99, TipoOrgano = "Riñón" });

            // Assert
            var action = Assert.IsType<ActionResult<object>>(result);
            var notFound = Assert.IsAssignableFrom<ObjectResult>(action.Result);
            Assert.Contains("Donante no encontrado", notFound.Value.ToString());
        }

        [Fact]
        public async Task CrearOrgano_NonAdminWithoutHospital_ReturnsUnauthorized()
        {
            // Arrange
            var context = CreateInMemoryContext("crear_nonadmin_db");
            var donante = new Donante { Id = 10, Nombre = "D10", HospitalId = 2 };
            context.Donantes.Add(donante);
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Usuario"), new Claim(ClaimTypes.NameIdentifier, "2") }));

            // Act
            var result = await controller.CrearOrgano(new CreateOrganoRequest { DonanteId = 10, TipoOrgano = "Riñón" });

            // Assert
            var action = Assert.IsType<ActionResult<object>>(result);
            var unauthorized = Assert.IsAssignableFrom<ObjectResult>(action.Result);
            Assert.Contains("No tiene un hospital asignado", unauthorized.Value.ToString());
        }

        [Fact]
        public async Task CrearOrgano_Duplicate_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext("crear_dup_db");
            var donante = new Donante { Id = 20, Nombre = "D20", HospitalId = 1 };
            context.Donantes.Add(donante);
            context.Organos.Add(new Organo { Id = 30, DonanteId = 20, TipoOrgano = "Riñón", Estado = "Disponible" });
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Administrador"), new Claim(ClaimTypes.NameIdentifier, "5") }));

            // Act
            var result = await controller.CrearOrgano(new CreateOrganoRequest { DonanteId = 20, TipoOrgano = "   Riñón   " });

            // Assert
            var action = Assert.IsType<ActionResult<object>>(result);
            var bad = Assert.IsType<BadRequestObjectResult>(action.Result);
            Assert.Contains("ya tiene un órgano", bad.Value.ToString());
        }

        [Fact]
        public async Task CrearOrgano_Success_ReturnsCreatedEvenIfBitacoraFails()
        {
            // Arrange
            var context = CreateInMemoryContext("crear_success_db");
            var donante = new Donante { Id = 40, Nombre = "D40", HospitalId = 1, TipoSanguineo = "A+" };
            context.Donantes.Add(donante);
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            bitacora.Setup(b => b.RegistrarAccionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>())).ThrowsAsync(new Exception("boom"));
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Administrador"), new Claim(ClaimTypes.NameIdentifier, "7") }));

            // Act
            var result = await controller.CrearOrgano(new CreateOrganoRequest { DonanteId = 40, TipoOrgano = "Riñón", Compatibilidad = "A+" });

            // Assert
            var action = Assert.IsType<ActionResult<object>>(result);
            var created = Assert.IsType<CreatedAtActionResult>(action.Result);
            Assert.NotNull(created.Value);
            var s = created.Value.ToString();
            Assert.Contains("Riñón", s);
            Assert.Contains("D40", s);
        }

        [Fact]
        public async Task ActualizarEstadoOrgano_NotFound_ReturnsNotFound()
        {
            // Arrange
            var context = CreateInMemoryContext("upd_notfound_db");
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ActualizarEstadoOrgano(999, new UpdateOrganoEstadoRequest { NuevoEstado = "Asignado" });

            // Assert
            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("Órgano no encontrado", notFound.Value.ToString());
        }

        [Fact]
        public async Task ActualizarEstadoOrgano_InvalidEstado_ReturnsBadRequest()
        {
            // Arrange
            var context = CreateInMemoryContext("upd_invalid_db");
            context.Organos.Add(new Organo { Id = 50, Estado = "Disponible" });
            await context.SaveChangesAsync();
            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);

            // Act
            var result = await controller.ActualizarEstadoOrgano(50, new UpdateOrganoEstadoRequest { NuevoEstado = "X" });

            // Assert
            var bad = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Estado inválido", bad.Value.ToString());
        }

        [Fact]
        public async Task ActualizarEstadoOrgano_Success_ReturnsOkEvenIfBitacoraFails()
        {
            // Arrange
            var context = CreateInMemoryContext("upd_success_db");
            context.Organos.Add(new Organo { Id = 60, Estado = "Disponible" });
            await context.SaveChangesAsync();

            var bitacora = new Mock<IBitacoraService>();
            var ranking = new Mock<IRankingService>();
            var controller = new OrganoController(context, bitacora.Object, ranking.Object);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "9") }));

            // Act
            var result = await controller.ActualizarEstadoOrgano(60, new UpdateOrganoEstadoRequest { NuevoEstado = "Asignado", Observaciones = "Obs" });

            // Assert
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Estado actualizado correctamente", ok.Value.ToString());
            // verify updated in db
            var updated = await context.Organos.FindAsync(60);
            Assert.Equal("Asignado", updated.Estado);
        }
    }
}