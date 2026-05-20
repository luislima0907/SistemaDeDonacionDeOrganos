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
        private readonly string _baseUrl = "http://localhost:5000";

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

        [Fact]
        public void Login_ValidCredentials_RedirectsToDashboard()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("medico1", "Medico123!");
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.Contains("dashboard", _driver.Url.ToLower());
        }

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

        [Fact]
        public void Login_ThenLogout_ReturnsToLoginPage()
        {
            // Arrange
            var loginPage = new LoginPage(_driver);
            var dashboardPage = new AdminDashboardPage(_driver);
            loginPage.Navigate(_baseUrl);

            // Act
            loginPage.Login("medico1", "Password123!");
            System.Threading.Thread.Sleep(2000);
            dashboardPage.ClickLogout();
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(loginPage.IsLoginFormVisible());
        }
    }
}