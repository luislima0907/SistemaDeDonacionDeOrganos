using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SistemaDonacion.Controllers;
using SistemaDonacion.Models;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Collections.Generic;

namespace SistemaDonacion.Tests.Unit.Controllers
{
	public class PacienteControllerTests
	{
		private AppDbContext CreateTestDbContext()
		{
			var options = new DbContextOptionsBuilder<AppDbContext>()
				.UseInMemoryDatabase(databaseName: $"TestDb_Paciente_{System.Guid.NewGuid()}")
				.Options;

			var context = new AppDbContext(options);

			// Seed hospitales
			context.Hospitales.AddRange(
				new Hospital { Id = 1, Nombre = "Hospital A", Ciudad = "CiudadA", Pais = "PA", Estado = true },
				new Hospital { Id = 2, Nombre = "Hospital B", Ciudad = "CiudadB", Pais = "PB", Estado = true }
			);

			// Seed pacientes
			context.Pacientes.AddRange(
				new Paciente { Id = 1, Nombre = "Paciente A1", TipoSanguineo = "O+", OrganoRequerido = "Corazón", NivelUrgencia = "Alta", HospitalId = 1, Estado = "Activo" },
				new Paciente { Id = 2, Nombre = "Paciente B1", TipoSanguineo = "A+", OrganoRequerido = "Hígado", NivelUrgencia = "Media", HospitalId = 2, Estado = "Activo" }
			);

			context.SaveChanges();
			return context;
		}

		private ClaimsPrincipal CreateUserPrincipal(int usuarioId, string role, int? hospitalId = null)
		{
			var claims = new List<Claim>
			{
				new Claim(ClaimTypes.NameIdentifier, usuarioId.ToString()),
				new Claim(ClaimTypes.Role, role)
			};
			if (hospitalId.HasValue)
				claims.Add(new Claim("HospitalId", hospitalId.Value.ToString()));

			var identity = new ClaimsIdentity(claims, "TestAuth");
			return new ClaimsPrincipal(identity);
		}

		[Fact]
		public async System.Threading.Tasks.Task GetPacientes_AsAdmin_ReturnsAllAsync()
		{
			var db = CreateTestDbContext();
			var mockBitacora = new Mock<IBitacoraService>();
			mockBitacora.Setup(b => b.RegistrarAccionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
				.Returns(System.Threading.Tasks.Task.CompletedTask);

			var controller = new PacienteController(db, mockBitacora.Object);
			controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(1, "Admin") } };

			var result = await controller.GetPacientes();
			var objectResult = result.Result as OkObjectResult;
			Assert.NotNull(objectResult);

			var value = objectResult.Value as System.Collections.IEnumerable;
			Assert.NotNull(value);
			// Debe devolver al menos los 2 pacientes seed
			var lista = value.Cast<object>().ToList();
			Assert.True(lista.Count >= 2);
		}

		[Fact]
		public async System.Threading.Tasks.Task GetPacientes_AsMedico_FiltersByHospitalAsync()
		{
			var db = CreateTestDbContext();
			var mockBitacora = new Mock<IBitacoraService>();
			mockBitacora.Setup(b => b.RegistrarAccionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
				.Returns(System.Threading.Tasks.Task.CompletedTask);

			var controller = new PacienteController(db, mockBitacora.Object);
			controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(10, "Medico", 1) } };

			var result = await controller.GetPacientes();
			var objectResult = result.Result as OkObjectResult;
			Assert.NotNull(objectResult);

			var value = objectResult.Value as System.Collections.IEnumerable;
			Assert.NotNull(value);
			var lista = value.Cast<object>().ToList();
			// Solo debe incluir pacientes del hospital 1 (usar reflexión para properties anónimas)
			foreach (var item in lista)
			{
				var prop = item.GetType().GetProperty("HospitalId");
				Assert.NotNull(prop);
				var val = prop.GetValue(item);
				Assert.Equal(1, Convert.ToInt32(val));
			}
		}

		[Fact]
		public async System.Threading.Tasks.Task GetPaciente_DifferentHospital_Returns403Async()
		{
			var db = CreateTestDbContext();
			var mockBitacora = new Mock<IBitacoraService>();
			mockBitacora.Setup(b => b.RegistrarAccionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
				.Returns(System.Threading.Tasks.Task.CompletedTask);

			var controller = new PacienteController(db, mockBitacora.Object);
			// Usuario pertenece al hospital 1
			controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(5, "Medico", 1) } };

			// Paciente con Id=2 pertenece al hospital 2
			var result = await controller.GetPaciente(2);
			var objectResult = result.Result as ObjectResult;
			Assert.NotNull(objectResult);
			Assert.Equal(403, objectResult.StatusCode);
		}

		[Fact]
		public async System.Threading.Tasks.Task CrearPaciente_InvalidTipoSanguineo_ReturnsBadRequestAsync()
		{
			var db = CreateTestDbContext();
			var mockBitacora = new Mock<IBitacoraService>();
			mockBitacora.Setup(b => b.RegistrarAccionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
				.Returns(System.Threading.Tasks.Task.CompletedTask);

			var controller = new PacienteController(db, mockBitacora.Object);
			controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(7, "Medico", 1) } };

			var request = new CreatePacienteRequest
			{
				Nombre = "Nuevo Paciente",
				TipoSanguineo = "ZZ",
				OrganoRequerido = "Corazón",
				NivelUrgencia = "Alta",
				HospitalId = 1
			};

			var result = await controller.CrearPaciente(request);
			var objectResult = result.Result as ObjectResult;
			Assert.NotNull(objectResult);
			Assert.Equal(400, objectResult.StatusCode);
		}

		[Fact]
		public async System.Threading.Tasks.Task CrearPaciente_ValidRequest_ReturnsCreatedAsync()
		{
			var db = CreateTestDbContext();
			var mockBitacora = new Mock<IBitacoraService>();
			mockBitacora.Setup(b => b.RegistrarAccionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>()))
				.Returns(System.Threading.Tasks.Task.CompletedTask);

			var controller = new PacienteController(db, mockBitacora.Object);
			controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = CreateUserPrincipal(9, "Medico", 1) } };

			var request = new CreatePacienteRequest
			{
				Nombre = "Paciente Nuevo",
				TipoSanguineo = "O+",
				OrganoRequerido = "Corazón",
				NivelUrgencia = "Alta",
				HospitalId = 1,
				Observaciones = "Prueba"
			};

			var result = await controller.CrearPaciente(request);
			var created = result.Result as CreatedAtActionResult;
			Assert.NotNull(created);
			// Verificar que el paciente fue agregado a la base de datos
			var pacienteEnDb = db.Pacientes.FirstOrDefault(p => p.Nombre == "Paciente Nuevo");
			Assert.NotNull(pacienteEnDb);
			Assert.Equal("O+", pacienteEnDb.TipoSanguineo);
		}
	}
}

