using Xunit;
using System.Net;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class DonanteControllerE2ETests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5135";

        public DonanteControllerE2ETests()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Verifica que la vista donantes.html exista
        /// o redirija al login si requiere autenticación.
        /// </summary>
        [Fact]
        public async Task DonantesHtml_RetornaOkORedireccion()
        {
            // Act
            var response = await _client.GetAsync("/donantes.html");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Redirect ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.Forbidden,
                $"Status inesperado: {response.StatusCode}");
        }

        /// <summary>
        /// Verifica endpoint de donantes activos.
        /// </summary>
        [Fact]
        public async Task GetDonantesActivos_RetornaStatusValido()
        {
            // Act
            var response = await _client.GetAsync("/api/donante/activos");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Status inesperado: {response.StatusCode}");
        }

        /// <summary>
        /// Verifica obtener donante por ID.
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
                $"Status inesperado: {response.StatusCode}");
        }
    }
}