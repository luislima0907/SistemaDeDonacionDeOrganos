using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SistemaDonacion.Tests.E2E.Pages
{
    public class RankingPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public RankingPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        private By TituloRanking => By.CssSelector("h1");
        private By TabRankingPorTipo => By.CssSelector("[data-tab='ranking-por-tipo']");
        private By TipoOrganoSelect => By.Id("tipoOrganoSelect");
        private By TipoSanguineoSelect => By.Id("tipoSanguineoSelect");
        private By BotonCargarRankingTipo => By.Id("btnCargarRankingTipo");
        private By ResultadoRankingTipo => By.Id("rankingTipoContainer");
        private By Mensaje => By.Id("message");
        private By LogoutButton => By.Id("logoutButton");
        private By BackMenuButton => By.Id("backMenuButton");

        public void Navigate(string baseUrl)
        {
            _driver.Navigate().GoToUrl($"{baseUrl}/ranking.html");
            Thread.Sleep(1000);
        }

        public bool IsRankingPageLoaded()
        {
            try
            {
                var titulo = _wait.Until(d => d.FindElement(TituloRanking));
                return titulo.Displayed && titulo.Text.Contains("Ranking");
            }
            catch
            {
                return false;
            }
        }

        public void ClickRankingPorTipoTab()
        {
            var tab = _wait.Until(d => d.FindElement(TabRankingPorTipo));
            ClickElement(tab);
            Thread.Sleep(500);
        }

        public void SeleccionarTipoOrgano(string tipoOrgano)
        {
            var selectElement = _wait.Until(d => d.FindElement(TipoOrganoSelect));
            SeleccionarOpcionPorTexto(selectElement, tipoOrgano);
            Thread.Sleep(500);
        }

        public void SeleccionarTipoSanguineo(string tipoSanguineo)
        {
            var selectElement = _wait.Until(d => d.FindElement(TipoSanguineoSelect));
            SeleccionarOpcionPorTexto(selectElement, tipoSanguineo);
            Thread.Sleep(500);
        }

        public void ClickCargarRankingTipo()
        {
            var button = _wait.Until(d => d.FindElement(BotonCargarRankingTipo));
            ClickElement(button);
            Thread.Sleep(1500);
        }

        public void ConsultarRankingPorTipo(string tipoOrgano, string tipoSanguineo)
        {
            ClickRankingPorTipoTab();
            SeleccionarTipoOrgano(tipoOrgano);
            SeleccionarTipoSanguineo(tipoSanguineo);
            ClickCargarRankingTipo();
        }

        public bool ExisteContenedorResultados()
        {
            try
            {
                var container = _wait.Until(d => d.FindElement(ResultadoRankingTipo));
                return container.Displayed;
            }
            catch
            {
                return false;
            }
        }

        public bool HayResultadoOMensaje()
        {
            try
            {
                Thread.Sleep(1000);

                var resultados = _driver.FindElements(ResultadoRankingTipo)
                    .FirstOrDefault(e => e.Displayed);

                var mensaje = _driver.FindElements(Mensaje)
                    .FirstOrDefault(e => e.Displayed && !string.IsNullOrWhiteSpace(e.Text));

                if (resultados != null && !string.IsNullOrWhiteSpace(resultados.Text))
                {
                    return true;
                }

                if (mensaje != null && !string.IsNullOrWhiteSpace(mensaje.Text))
                {
                    return true;
                }

                return resultados != null;
            }
            catch
            {
                return false;
            }
        }

        public bool ContieneTextoEsperado()
        {
            try
            {
                var bodyText = _driver.FindElement(By.TagName("body")).Text.ToLower();

                return bodyText.Contains("ranking") ||
                       bodyText.Contains("paciente") ||
                       bodyText.Contains("compatible") ||
                       bodyText.Contains("puntaje") ||
                       bodyText.Contains("prioridad") ||
                       bodyText.Contains("no se encontraron") ||
                       bodyText.Contains("sin resultados");
            }
            catch
            {
                return false;
            }
        }

        public void ClickVolverMenu()
        {
            try
            {
                var button = _wait.Until(d => d.FindElement(BackMenuButton));
                ClickElement(button);
                Thread.Sleep(1000);
            }
            catch
            {
                var possibleButton = _driver.FindElements(By.XPath("//button | //a"))
                    .FirstOrDefault(e =>
                        e.Displayed &&
                        (e.Text.ToLower().Contains("volver") ||
                         e.Text.ToLower().Contains("regresar") ||
                         e.Text.ToLower().Contains("menú") ||
                         e.Text.ToLower().Contains("menu")));

                if (possibleButton != null)
                {
                    ClickElement(possibleButton);
                    Thread.Sleep(1000);
                }
            }
        }

        public void ClickLogout()
        {
            try
            {
                var button = _wait.Until(d => d.FindElement(LogoutButton));
                ClickElement(button);
                Thread.Sleep(1000);
            }
            catch
            {
                var possibleButton = _driver.FindElements(By.XPath("//button | //a"))
                    .FirstOrDefault(e =>
                        e.Displayed &&
                        (e.Text.ToLower().Contains("cerrar") ||
                         e.Text.ToLower().Contains("logout") ||
                         e.Text.ToLower().Contains("salir")));

                if (possibleButton != null)
                {
                    ClickElement(possibleButton);
                    Thread.Sleep(1000);
                }
            }
        }

        private void SeleccionarOpcionPorTexto(IWebElement selectElement, string texto)
        {
            var opciones = selectElement.FindElements(By.TagName("option"));

            var opcion = opciones.FirstOrDefault(o =>
                o.Text.Trim().Equals(texto, StringComparison.OrdinalIgnoreCase));

            if (opcion != null)
            {
                opcion.Click();
                return;
            }

            ((IJavaScriptExecutor)_driver).ExecuteScript(@"
                const select = arguments[0];
                const texto = arguments[1].toLowerCase();

                for (let i = 0; i < select.options.length; i++) {
                    if (select.options[i].text.toLowerCase() === texto) {
                        select.selectedIndex = i;
                        select.dispatchEvent(new Event('change'));
                        break;
                    }
                }
            ", selectElement, texto);
        }

        private void ClickElement(IWebElement element)
        {
            try
            {
                element.Click();
            }
            catch
            {
                ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
            }
        }
    }
}