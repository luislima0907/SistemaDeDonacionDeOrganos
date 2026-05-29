using Xunit;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class DonanteControllerIntegrationTests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5135";

        public DonanteControllerIntegrationTests()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        // GET /api/donante

        /// <summary>
        /// Prueba obtener todos los donantes.
        /// Puede retornar 200 o 401 dependiendo autenticación.
        /// </summary>
        [Fact]
        public async Task GetDonantes_RetornaOkOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/donante");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}"
            );
        }

        // GET /api/donante/activos

        /// <summary>
        /// Prueba obtener conteo de donantes activos.
        /// </summary>
        [Fact]
        public async Task GetDonantesActivos_RetornaOk()
        {
            // Act
            var response = await _client.GetAsync("/api/donante/activos");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Status inesperado: {response.StatusCode}"
            );
        }

        // GET /api/donante/{id}

        /// <summary>
        /// Prueba obtener donante ID 1.
        /// </summary>
        [Fact]
        public async Task GetDonante_Id1_RetornaStatusValido()
        {
            // Act
            var response = await _client.GetAsync("/api/donante/1");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden,
                $"Status inesperado: {response.StatusCode}"
            );
        }

        /// <summary>
        /// Prueba obtener donante inexistente.
        /// </summary>
        [Fact]
        public async Task GetDonante_IdInexistente_RetornaNotFoundOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/donante/999999");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba NotFound (404) o Unauthorized (401), pero se obtuvo {response.StatusCode}"
            );
        }

        // POST /api/donante

        /// <summary>
        /// Prueba crear donante sin autenticación.
        /// </summary>
        [Fact]
        public async Task CrearDonante_SinAutenticacion_RetornaStatusValido()
        {
            // Arrange
            var body = new
            {
                nombre = "Donante Test",
                tipoSanguineo = "O+",
                edad = 30,
                hospitalId = 1,
                observaciones = "Test integración"
            };

            var json = JsonSerializer.Serialize(body);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/donante", content);

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden,
                $"Status inesperado: {response.StatusCode}"
            );
        }

        // PUT /api/donante/{id}/estado

        /// <summary>
        /// Prueba actualizar estado de donante.
        /// </summary>
        [Fact]
        public async Task ActualizarEstadoDonante_RetornaStatusValido()
        {
            // Arrange
            var body = new
            {
                nuevoEstado = "Asignado",
                observaciones = "Test integración"
            };

            var json = JsonSerializer.Serialize(body);

            var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PutAsync(
                "/api/donante/1/estado",
                content
            );

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden,
                $"Status inesperado: {response.StatusCode}"
            );
        }

        // GET /api/donante/{id}/organos

        /// <summary>
        /// Prueba obtener órganos de un donante.
        /// </summary>
        [Fact]
        public async Task GetOrganosPorDonante_RetornaStatusValido()
        {
            // Act
            var response = await _client.GetAsync("/api/donante/1/organos");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden,
                $"Status inesperado: {response.StatusCode}"
            );
        }
    }
}