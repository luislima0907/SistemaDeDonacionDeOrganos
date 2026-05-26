using Xunit;
using System.Net;
using System.Text.Json;
using SistemaDonacion.Models;
using LoginRequest = SistemaDonacion.Models.LoginRequest;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class AuthControllerIntegrationTests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5135"; // Cambia el puerto si es necesario

        public AuthControllerIntegrationTests()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Prueba login con credenciales válidas - Usuario medico1
        /// Username: medico1
        /// Password: Medico123!
        /// Requisito: La aplicación debe estar ejecutándose en http://localhost:5000
        /// </summary>
        [Fact]
        public async Task Login_WithValidCredentials_Medico1_ReturnsOk()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "medico1",
                Password = "Medico123!"
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
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("OK", responseContent);
            Assert.Contains("Medico", responseContent);
        }

        /// <summary>
        /// Prueba login con credenciales válidas - Usuario admin
        /// Username: admin
        /// Password: Admin123!
        /// </summary>
        [Fact]
        public async Task Login_WithValidCredentials_Admin_ReturnsOk()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "admin2",
                Password = "Medico123!"
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
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("OK", responseContent);
            Assert.Contains("Administrador", responseContent);
        }

        /// <summary>
        /// Prueba login con credenciales válidas - Usuario medico2
        /// Username: medico2
        /// Password: Medico123!
        /// </summary>
        [Fact]
        public async Task Login_WithValidCredentials_Medico2_ReturnsOk()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "medico2",
                Password = "Medico123!"
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
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("OK", responseContent);
            Assert.Contains("Medico", responseContent);
        }

        /// <summary>
        /// Prueba login con credenciales válidas - Usuario admin2
        /// Username: admin2
        /// Password: Medico123!
        /// </summary>
        [Fact]
        public async Task Login_WithValidCredentials_Admin2_ReturnsOk()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "admin2",
                Password = "Medico123!"
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
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.Contains("OK", responseContent);
            Assert.Contains("Administrador", responseContent);
        }

        /// <summary>
        /// Prueba login con username inexistente
        /// </summary>
        [Fact]
        public async Task Login_WithInvalidUsername_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "usuarioQueNoExiste123456",
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

        /// <summary>
        /// Prueba login con contraseña incorrecta
        /// </summary>
        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new LoginRequest
            {
                Username = "medico1",
                Password = "ContraseñaIncorrecta123"
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

        /// <summary>
        /// Prueba obtener usuario actual sin autenticación
        /// </summary>
        [Fact]
        public async Task GetCurrentUser_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/auth/current");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /// <summary>
        /// Prueba check session sin autenticación
        /// </summary>
        [Fact]
        public async Task CheckSession_WithoutAuthentication_ReturnsResponse()
        {
            // Act
            var response = await _client.GetAsync("/api/auth/check-session");

            // Assert
            // El endpoint retorna OK incluso sin autenticación
            // Verificar que retorna una respuesta válida
            Assert.NotNull(response);
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
            
            var responseContent = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(responseContent);
        }

        /// <summary>
        /// Prueba login con flow completo
        /// </summary>
        [Fact]
        public async Task Login_CompleteFlow_LoginAndLogout_Success()
        {
            // Arrange - Crear un nuevo HttpClient con manejo de cookies
            var handler = new HttpClientHandler { UseCookies = true, CookieContainer = new System.Net.CookieContainer() };
            var clientWithCookies = new HttpClient(handler)
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            clientWithCookies.DefaultRequestHeaders.Add("Accept", "application/json");

            var loginRequest = new LoginRequest
            {
                Username = "medico1",
                Password = "Medico123!"
            };
            var loginContent = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            // Act - Login
            var loginResponse = await clientWithCookies.PostAsync("/api/auth/login", loginContent);

            // Assert - Verificar login exitoso
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
            var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
            Assert.Contains("OK", loginResponseContent);

            // Act - Check session
            var sessionResponse = await clientWithCookies.GetAsync("/api/auth/check-session");

            // Assert - Verificar sesión activa
            Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);

            // Cleanup
            handler.Dispose();
            clientWithCookies.Dispose();
        }
    }
}