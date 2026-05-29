using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using SistemaDonacion.Tests.E2E.Pages;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using WebDriverManager.Helpers;
using Xunit;
namespace SistemaDonacion.Tests.E2E.Controllers
{
    public class OrganoControllerTests : IAsyncLifetime
    {
        private IWebDriver _driver;
        private readonly string _baseUrl = "http://localhost:5135";

        public async Task InitializeAsync()
        {
            // Forzar ChromeDriver 148 para que coincida con tu Chrome instalado
            var config = new ChromeConfig();
            new DriverManager().SetUpDriver(config, "148.0.7778.178");

            var service = ChromeDriverService.CreateDefaultService();
            service.SuppressInitialDiagnosticInformation = true;

            var options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");

            _driver = new ChromeDriver(service, options);
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _driver?.Quit();
            _driver?.Dispose();
            await Task.CompletedTask;
        }


        // Helper: login como admin y navegar a donantes.html
       
        private void LoginComoAdminYNavegarAOrganos()
        {
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);
            loginPage.Login("admin", "Admin123!");
            System.Threading.Thread.Sleep(2000);

            _driver.Navigate().GoToUrl($"{_baseUrl}/donantes.html");
            System.Threading.Thread.Sleep(2000);
        }

       
      
        // Usa el selector exacto del HTML: button.tab-btn
        private void ClickTabBtn(string textoTab)
        {
            var tabs = _driver.FindElements(By.CssSelector("button.tab-btn"));
            var tab = tabs.FirstOrDefault(t => t.Text.Trim() == textoTab);
            if (tab != null)
            {
                tab.Click();
                System.Threading.Thread.Sleep(1000);
            }
        }

        // TEST 1: Sin login redirige al login
         
        /// <summary>
        /// Sin autenticación, donantes.html debe redirigir al login.
        /// </summary>
        [Fact]
        public void AccederOrganos_SinLogin_RedirigaALogin()
        {
            // Act
            _driver.Navigate().GoToUrl($"{_baseUrl}/donantes.html");
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(
                _driver.Url.Contains("login"),
                $"Sin autenticación debería redirigir al login. URL actual: {_driver.Url}");
        }

        // TEST 2: El dashboard muestra el widget de órganos disponibles
  

        /// <summary>
        /// El dashboard admin muestra "ÓRGANOS DISPONIBLES".
        /// </summary>
        [Fact]
        public void Dashboard_ComoAdmin_MuestraContadorDeOrganos()
        {
            // Arrange & Act
            var loginPage = new LoginPage(_driver);
            loginPage.Navigate(_baseUrl);
            loginPage.Login("admin", "Admin123!");
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.Contains("admin.html", _driver.Url.ToLower());
            var pageSource = _driver.PageSource;
            Assert.True(
                pageSource.Contains("ÓRGANOS DISPONIBLES") ||
                pageSource.Contains("Órganos Disponibles") ||
                pageSource.Contains("rganos"),
                "El dashboard debería mostrar el widget de órganos disponibles");
        }


        // TEST 3: Los 4 tabs de donantes.html están visibles


        /// <summary>
        /// Verifica que los 4 tabs existen: Registrar Donante,
        /// Listar Donantes, Registrar Órgano, Listar Órganos.
        /// </summary>
        [Fact]
        public void DonantesHtml_ComoAdmin_MuestraLosCuatroTabs()
        {
            // Arrange & Act
            LoginComoAdminYNavegarAOrganos();

            var tabs = _driver.FindElements(By.CssSelector("button.tab-btn"));

            // Assert — deben existir los 4 tabs
            Assert.True(tabs.Count >= 4, $"Se esperaban 4 tabs, se encontraron {tabs.Count}");

            var textosTabs = tabs.Select(t => t.Text.Trim()).ToList();
            Assert.Contains("Registrar Donante", textosTabs);
            Assert.Contains("Listar Donantes", textosTabs);
            Assert.Contains("Registrar Órgano", textosTabs);
            Assert.Contains("Listar Órganos", textosTabs);
        }


        // TEST 4: Tab "Registrar Órgano" muestra el formulario


        /// <summary>
        /// Al hacer click en "Registrar Órgano", aparece el formulario
        /// con los campos donanteId y tipoOrgano.
        /// </summary>
        [Fact]
        public void TabRegistrarOrgano_ComoAdmin_MuestraFormulario()
        {
            // Arrange
            LoginComoAdminYNavegarAOrganos();

            // Act — click en "Registrar Órgano"
            ClickTabBtn("Registrar Órgano");

            // Assert — el div #registrar-organo debe estar activo
            // y el formulario #formOrgano debe ser visible
            var formOrgano = _driver.FindElement(By.Id("formOrgano"));
            Assert.NotNull(formOrgano);

            // Verificar que los campos clave existen
            var selectDonante = _driver.FindElement(By.Id("donanteId"));
            var selectTipoOrgano = _driver.FindElement(By.Id("tipoOrgano"));
            Assert.NotNull(selectDonante);
            Assert.NotNull(selectTipoOrgano);

            // Verificar que el select de tipo tiene las opciones correctas
            var opciones = selectTipoOrgano.FindElements(By.TagName("option"));
            var textos = opciones.Select(o => o.Text).ToList();
            Assert.Contains("Corazón", textos);
            Assert.Contains("Riñón", textos);
            Assert.Contains("Hígado", textos);
        }

       
        // TEST 5: Tab "Listar Órganos" muestra el contenedor
     

        /// <summary>
        /// Al hacer click en "Listar Órganos", aparece el div
        /// #organos-container con el botón de actualizar.
        /// </summary>
        [Fact]
        public void TabListarOrganos_ComoAdmin_MuestraContenedor()
        {
            // Arrange
            LoginComoAdminYNavegarAOrganos();

            // Act — click en "Listar Órganos"
            ClickTabBtn("Listar Órganos");

            // Assert — el contenedor #organos-container debe existir
            var contenedor = _driver.FindElement(By.Id("organos-container"));
            Assert.NotNull(contenedor);

            // Verificar que el botón "Actualizar" existe
            var btnActualizar = _driver.FindElements(By.CssSelector("button.btn-secondary"))
                .FirstOrDefault(b => b.Text.Contains("Actualizar"));
            Assert.NotNull(btnActualizar);
        }
    }
}
