using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SistemaDonacion.Tests.E2E.Pages;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Xunit;

namespace SistemaDonacion.Tests.E2E.Tests
{
    public class RankingE2ETests : IAsyncLifetime
    {
        private IWebDriver _driver = null!;
        private readonly string _baseUrl = "https://localhost:7019";

        public async Task InitializeAsync()
        {
            new DriverManager().SetUpDriver(new ChromeConfig());

            var options = new ChromeOptions();
            options.AcceptInsecureCertificates = true;
            options.AddArgument("--ignore-certificate-errors");
            options.AddArgument("--allow-insecure-localhost");
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            options.AddArgument("--window-size=1366,768");

            _driver = new ChromeDriver(options);

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _driver?.Quit();
            _driver?.Dispose();

            await Task.CompletedTask;
        }

        private void LoginConUsuariosDePrueba()
        {
            var loginPage = new LoginPage(_driver);

            var usuarios = new List<(string Usuario, string Contrasenia)>
            {
                ("admin", "Admin123!"),
                ("admin2", "Medico123!"),
                ("medico1", "Medico123!"),
                ("medico2", "Medico123!")
            };

            foreach (var usuario in usuarios)
            {
                loginPage.Navigate(_baseUrl);
                Thread.Sleep(1000);

                loginPage.Login(usuario.Usuario, usuario.Contrasenia);
                Thread.Sleep(3000);

                if (!_driver.Url.ToLower().Contains("login"))
                {
                    return;
                }
            }

            throw new Exception("No se pudo iniciar sesión con ninguno de los usuarios de prueba.");
        }

        private void IrARankingDespuesDeLogin()
        {
            LoginConUsuariosDePrueba();

            _driver.Navigate().GoToUrl($"{_baseUrl}/ranking.html");
            Thread.Sleep(2500);

            if (_driver.Url.ToLower().Contains("login"))
            {
                throw new Exception("La página ranking.html redirigió al login. No hay sesión activa.");
            }
        }

        private void AbrirRankingPorTipo()
        {
            var posiblesTabs = _driver.FindElements(By.XPath("//button | //a | //div"))
                .Where(e => e.Displayed && e.Text.Contains("Ranking por Tipo"))
                .ToList();

            if (posiblesTabs.Any())
            {
                posiblesTabs.First().Click();
                Thread.Sleep(1000);
            }
        }

        [Fact]
        public void RankingPage_LoadsCorrectly()
        {
            IrARankingDespuesDeLogin();

            var bodyText = _driver.FindElement(By.TagName("body")).Text;

            Assert.Contains("Ranking", bodyText);
        }

        [Fact]
        public void RankingPage_ShowsMainTitle()
        {
            IrARankingDespuesDeLogin();

            var title = _driver.FindElement(By.TagName("h1")).Text;

            Assert.Contains("Ranking", title);
        }

        [Fact]
        public void RankingPage_ShowsRankingPorTipoTab()
        {
            IrARankingDespuesDeLogin();

            var bodyText = _driver.FindElement(By.TagName("body")).Text;

            Assert.Contains("Ranking por Tipo", bodyText);
        }

        [Fact]
        public void RankingPage_HasTipoOrganoSelect()
        {
            IrARankingDespuesDeLogin();
            AbrirRankingPorTipo();

            var elementos = _driver.FindElements(By.Id("tipoOrganoSelect"));

            Assert.True(elementos.Count > 0);
        }

        [Fact]
        public void RankingPage_HasTipoSanguineoSelect()
        {
            IrARankingDespuesDeLogin();
            AbrirRankingPorTipo();

            var elementos = _driver.FindElements(By.Id("tipoSanguineoSelect"));

            Assert.True(elementos.Count > 0);
        }

        [Fact]
        public void RankingPage_HasCargarRankingTipoButton()
        {
            IrARankingDespuesDeLogin();
            AbrirRankingPorTipo();

            var botones = _driver.FindElements(By.XPath("//button | //a | //input"))
                .Where(e =>
                    e.Displayed &&
                    (
                        e.Text.ToLower().Contains("cargar") ||
                        e.Text.ToLower().Contains("consultar") ||
                        e.Text.ToLower().Contains("ranking") ||
                        e.GetAttribute("value")?.ToLower().Contains("cargar") == true ||
                        e.GetAttribute("value")?.ToLower().Contains("consultar") == true ||
                        e.GetAttribute("id")?.ToLower().Contains("ranking") == true
                    )
                )
                .ToList();

            Assert.True(botones.Count > 0);
        }

        [Fact]
        public void RankingPage_HasBackMenuButton()
        {
            IrARankingDespuesDeLogin();

            var elementos = _driver.FindElements(By.Id("backMenuButton"));

            Assert.True(elementos.Count > 0);
        }

        [Fact]
        public void RankingPage_HasLogoutButton()
        {
            IrARankingDespuesDeLogin();

            var elementos = _driver.FindElements(By.Id("logoutButton"));

            Assert.True(elementos.Count > 0);
        }
    }
}