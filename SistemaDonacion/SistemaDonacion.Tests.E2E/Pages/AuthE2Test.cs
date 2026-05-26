using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;
using SistemaDonacion.Tests.E2E.Pages;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace SistemaDonacion.Tests.E2E.Tests
{
    public class AuthE2ETests : IAsyncLifetime
    {
        private IWebDriver _driver;
        private readonly string _baseUrl = "http://localhost:5135";

        public async Task InitializeAsync()
        {
            new DriverManager().SetUpDriver(new ChromeConfig());
            var options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            _driver = new ChromeDriver(options);
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _driver?.Quit();
            _driver?.Dispose();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Prueba login exitoso como medico1
        /// Username: medico1
        /// Password: Medico123!
        /// Redirige a: http://localhost:5135/medico.html
        /// </summary>
        [Fact]
        public void Login_AsMedico1_RedirectsToMedicoDashboard()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("medico1", "Medico123!");
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.Contains("medico.html", _driver.Url.ToLower());
        }

        /// <summary>
        /// Prueba login exitoso como medico2
        /// Username: medico2
        /// Password: MedicoDos123!
        /// Redirige a: http://localhost:5135/medico.html
        /// </summary>
        [Fact]
        public void Login_AsMedico2_RedirectsToMedicoDashboard()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.Contains("medico.html", _driver.Url.ToLower());
        }

        /// <summary>
        /// Prueba login exitoso como admin2
        /// Username: admin2
        /// Password: AdminDos123!
        /// Redirige a: http://localhost:5135/admin.html
        /// </summary>
        [Fact]
        public void Login_AsAdmin2_RedirectsToAdminDashboard()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("admin2", "Medico123!");
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.Contains("admin.html", _driver.Url.ToLower());
        }

        /// <summary>
        /// Prueba login con credenciales inválidas
        /// </summary>
        [Fact]
        public void Login_InvalidCredentials_ShowsErrorMessage()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("usuarioInexistente", "WrongPassword");
            System.Threading.Thread.Sleep(1000);

            // Assert
            var errorMessage = loginPage.GetErrorMessage();
            Assert.NotEmpty(errorMessage);
        }

        /// <summary>
        /// Prueba login con contraseña incorrecta
        /// </summary>
        [Fact]
        public void Login_CorrectUsernameWrongPassword_ShowsErrorMessage()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("medico1", "PasswordIncorrecta");
            System.Threading.Thread.Sleep(1000);

            // Assert
            var errorMessage = loginPage.GetErrorMessage();
            Assert.NotEmpty(errorMessage);
        }

        /// <summary>
        /// Prueba flujo completo médico: login -> medico.html -> logout
        /// </summary>
        [Fact]
        public void Login_MedicoThenLogout_ReturnsToLoginPage()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            var dashboardPage = new AdminDashboardPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("medico1", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            
            // Verificar que está en medico.html
            Assert.Contains("medico.html", _driver.Url.ToLower());
            
            dashboardPage.ClickLogout();
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(loginPage.IsLoginFormVisible());
        }

        /// <summary>
        /// Prueba flujo completo admin: login -> admin.html -> logout
        /// </summary>
        [Fact]
        public void Login_AdminThenLogout_ReturnsToLoginPage()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            var dashboardPage = new AdminDashboardPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("admin2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            
            // Verificar que está en admin.html
            Assert.Contains("admin.html", _driver.Url.ToLower());
            
            dashboardPage.ClickLogout();
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(loginPage.IsLoginFormVisible());
        }
    }
}