using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using SistemaDonacion.Models;
using LoginRequest = SistemaDonacion.Models.LoginRequest;
using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class PacienteControllerIntegrationTests : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        public async Task InitializeAsync()
        {
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseSetting("ContentRoot",
                        Path.GetDirectoryName(typeof(Program).Assembly.Location) ?? "");

                    builder.ConfigureServices(services =>
                    {
                        services.PostConfigure<CookieAuthenticationOptions>(
                            CookieAuthenticationDefaults.AuthenticationScheme, options =>
                            {
                                options.Cookie.SecurePolicy = CookieSecurePolicy.None;
                                options.Cookie.SameSite = SameSiteMode.Lax;
                            });
                    });
                });

            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                HandleCookies = true,
                AllowAutoRedirect = false
            });

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            _factory.Dispose();
            await Task.CompletedTask;
        }

        // iniciar sesión y obtener cookie de sesión
        private async Task<bool> LoginAsync(string username, string password)
        {
            var loginRequest = new
            {
                username,  // minúscula para que coincida con el modelo
                password
            };
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            var response = await _client.PostAsync("/api/auth/login", content);
            return response.IsSuccessStatusCode;
        }
        // Método helper: crear un paciente de prueba
        private StringContent CrearRequestPaciente(
            string nombre = "Paciente Test",
            string tipoSanguineo = "O+",
            string organoRequerido = "Riñón",
            string nivelUrgencia = "Alta",
            int hospitalId = 0)
        {
            var request = new
            {
                nombre,
                tipoSanguineo,
                organoRequerido,
                nivelUrgencia,
                hospitalId
            };
            return new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json"
            );
        }

        // GET api/paciente — Listar pacientes

        [Fact]
        public async Task GetPacientes_SinAutenticacion_RetornaUnauthorizedORedirect()
        {
            // Arrange — cliente sin sesión iniciada
            var clienteSinSesion = _factory.CreateClient(
                new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
            );

            // Act
            var response = await clienteSinSesion.GetAsync("/api/paciente");

            // Assert — sin sesión debe rechazar el accesos
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Redirect,
                $"Se esperaba Unauthorized o Redirect pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task GetPacientes_ComoMedico_RetornaOk()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");

            // Act
            var response = await _client.GetAsync("/api/paciente");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task GetPacientes_ComoAdmin_RetornaOk()
        {
            // Arrange
            await LoginAsync("admin", "Admin123!");

            // Act
            var response = await _client.GetAsync("/api/paciente");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        // POST api/paciente — Registrar paciente

        [Fact]
        public async Task CrearPaciente_ConDatosValidos_RetornaCreated()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");
            var content = CrearRequestPaciente();

            // Act
            var response = await _client.PostAsync("/api/paciente", content);

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task CrearPaciente_SinAutenticacion_RetornaUnauthorized()
        {
            // Arrange — cliente nuevo sin sesión
            var clienteSinSesion = _factory.CreateClient();
            var content = CrearRequestPaciente();

            // Act
            var response = await clienteSinSesion.PostAsync("/api/paciente", content);

            // Assert
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Redirect,
                $"Se esperaba Unauthorized pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task CrearPaciente_ConTipoSanguineoInvalido_ReturnsBadRequest()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");
            var content = CrearRequestPaciente(tipoSanguineo: "Z+");

            // Act
            var response = await _client.PostAsync("/api/paciente", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CrearPaciente_ConOrganoInvalido_ReturnsBadRequest()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");
            var content = CrearRequestPaciente(organoRequerido: "OrganNoExiste");

            // Act
            var response = await _client.PostAsync("/api/paciente", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CrearPaciente_ConNivelUrgenciaInvalido_ReturnsBadRequest()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");
            var content = CrearRequestPaciente(nivelUrgencia: "Extrema");

            // Act
            var response = await _client.PostAsync("/api/paciente", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CrearPaciente_ConNombreVacio_ReturnsBadRequest()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");
            var content = CrearRequestPaciente(nombre: "");

            // Act
            var response = await _client.PostAsync("/api/paciente", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CrearPaciente_EnHospitalDeOtroMedico_ReturnsForbidden()
        {
            // Arrange — medico1 es del hospital 1, intenta registrar en hospital 2
            await LoginAsync("medico2", "Medico123!");
            var content = CrearRequestPaciente(hospitalId: 2);

            // Act
            var response = await _client.PostAsync("/api/paciente", content);

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // GET api/paciente/{id} — Obtener paciente por ID

        [Fact]
        public async Task GetPaciente_ConIdInexistente_ReturnsNotFound()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");

            // Act
            var response = await _client.GetAsync("/api/paciente/99999");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPaciente_DeOtroHospital_ReturnsForbidden()
        {
            // Arrange
            // Primero crear un paciente con medico2 (hospital 2)
            await LoginAsync("medico2", "Medico123!");
            var contentCrear = CrearRequestPaciente(nombre: "Paciente Hospital 2");
            var responseCrear = await _client.PostAsync("/api/paciente", contentCrear);
            Assert.Equal(HttpStatusCode.Created, responseCrear.StatusCode);

            var bodyCrear = await responseCrear.Content.ReadAsStringAsync();
            var pacienteCreado = JsonSerializer.Deserialize<JsonElement>(bodyCrear);
            var pacienteId = pacienteCreado.GetProperty("id").GetInt32();

            // Ahora intentar acceder como medico2 (hospital 2)
            var clienteMedico2 = _factory.CreateClient();
            await LoginConCliente(clienteMedico2, "medico2", "Medico123!");

            // Act
            var response = await clienteMedico2.GetAsync($"/api/paciente/{pacienteId}");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        // PUT api/paciente/{id}/estado — Actualizar estado

        [Fact]
        public async Task ActualizarEstado_ConEstadoValido_ReturnsOk()
        {
            // Arrange — crear paciente primero
            await LoginAsync("medico2", "Medico123!");
            var contentCrear = CrearRequestPaciente(nombre: "Paciente Para Estado");
            var responseCrear = await _client.PostAsync("/api/paciente", contentCrear);
            Assert.Equal(HttpStatusCode.Created, responseCrear.StatusCode);

            var body = await responseCrear.Content.ReadAsStringAsync();
            var paciente = JsonSerializer.Deserialize<JsonElement>(body);
            var pacienteId = paciente.GetProperty("id").GetInt32();

            var requestEstado = new { nuevoEstado = "Inactivo", observaciones = "Prueba de integración" };
            var contentEstado = new StringContent(
                JsonSerializer.Serialize(requestEstado),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/paciente/{pacienteId}/estado", contentEstado);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ActualizarEstado_ConEstadoInvalido_ReturnsBadRequest()
        {
            // Arrange — crear paciente primero
            await LoginAsync("medico2", "Medico123!");
            var contentCrear = CrearRequestPaciente(nombre: "Paciente Estado Invalido");
            var responseCrear = await _client.PostAsync("/api/paciente", contentCrear);
            Assert.Equal(HttpStatusCode.Created, responseCrear.StatusCode);

            var body = await responseCrear.Content.ReadAsStringAsync();
            var paciente = JsonSerializer.Deserialize<JsonElement>(body);
            var pacienteId = paciente.GetProperty("id").GetInt32();

            var requestEstado = new { nuevoEstado = "EstadoQueNoExiste" };
            var contentEstado = new StringContent(
                JsonSerializer.Serialize(requestEstado),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync($"/api/paciente/{pacienteId}/estado", contentEstado);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // GET api/paciente/activos — Conteo de pacientes activos

        [Fact]
        public async Task GetPacientesActivos_RetornaOkConConteo()
        {
            // Arrange
            await LoginAsync("medico2", "Medico123!");

            // Act
            var response = await _client.GetAsync("/api/paciente/activos");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var resultado = JsonSerializer.Deserialize<JsonElement>(body);
            Assert.True(resultado.TryGetProperty("count", out _),
                "La respuesta debe contener la propiedad 'count'");
        }

        // para login con cliente específico
        private async Task LoginConCliente(HttpClient cliente, string username, string password)
        {
            var loginRequest = new LoginRequest
            {
                Username = username,
                Password = password
            };
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            await cliente.PostAsync("/api/auth/login", content);
        }
        [Fact]
        public async Task Debug_Login_FuncionaCorrectamente()
        {
            var loginRequest = new { username = "medico1", password = "Medico123!" };
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await _client.PostAsync("/api/auth/login", content);
            var body = await response.Content.ReadAsStringAsync();
            
            // Ver qué devuelve exactamente
            Assert.True(response.IsSuccessStatusCode, 
                $"Login falló: {response.StatusCode} - {body}");
        }
    }
}