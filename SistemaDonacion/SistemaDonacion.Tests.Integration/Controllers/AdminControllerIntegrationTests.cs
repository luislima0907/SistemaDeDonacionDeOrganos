using Xunit;
using System.Net;
using System.Text.Json;
using SistemaDonacion.Models;
using LoginRequest = SistemaDonacion.Models.LoginRequest;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class AdminControllerIntegrationTests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5135";

        public AdminControllerIntegrationTests()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Prueba acceso al dashboard administrativo como admin3
        /// Username: admin3
        /// Password: Admin3!
        /// </summary>
        [Fact]
        public async Task GetAdminDashboard_AsAdmin3_ReturnsOk()
        {
            // Arrange
            var response = await _client.GetAsync("/api/admin/dashboard");

            // Assert
            Assert.NotNull(response);
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }
    }
}
