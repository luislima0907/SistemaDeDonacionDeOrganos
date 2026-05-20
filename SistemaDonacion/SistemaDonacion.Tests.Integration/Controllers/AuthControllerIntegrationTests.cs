using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using SistemaDonacion;
using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using SistemaDonacion.Models;
using LoginRequest = SistemaDonacion.Models.LoginRequest;



namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class AuthControllerIntegrationTests : IAsyncLifetime
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
                });
            _client = _factory.CreateClient();
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client.Dispose();
            _factory.Dispose();
            await Task.CompletedTask;
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOk()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "medico1",
                Password = "Password123!"
            };
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "usuarioInexistente",
                Password = "Password123!"
            };
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithInactiveUser_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "inactiveuser",
                Password = "Password123!"
            };
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Act
            var response = await _client.PostAsync("/api/auth/login", content);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}