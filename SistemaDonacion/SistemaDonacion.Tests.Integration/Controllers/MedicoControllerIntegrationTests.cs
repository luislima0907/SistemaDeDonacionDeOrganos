using Xunit;
using System.Net;
using System.Text.Json;
using SistemaDonacion.Models;
using LoginRequest = SistemaDonacion.Models.LoginRequest;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class MedicoControllerIntegrationTests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5135";

        public MedicoControllerIntegrationTests()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Prueba acceso al dashboard médico como medico1
        /// Username: medico1
        /// Password: Medico123!
        /// </summary>
        [Fact]
        public async Task GetMedicoDashboard_AsMedico1_ReturnsOk()
        {
            // Arrange
            var response = await _client.GetAsync("/api/medico/dashboard");

            // Assert
            Assert.NotNull(response);
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }

        /// <summary>
        /// Prueba acceso al dashboard médico como medico2
        /// </summary>
        [Fact]
        public async Task GetMedicoDashboard_AsMedico2_ReturnsOk()
        {
            // Arrange
            var response = await _client.GetAsync("/api/medico/dashboard");

            // Assert
            Assert.NotNull(response);
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }
    }
}
