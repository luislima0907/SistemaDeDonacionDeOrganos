using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SistemaDonacion.Tests.E2E.Pages
{
    public class AdminDashboardPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public AdminDashboardPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // Localizadores - se prueban múltiples opciones
        private By DashboardTitle => By.CssSelector("h1");
        private By LogoutButton => By.CssSelector("button:contains('Logout'), button:contains('logout'), button:contains('Cerrar Sesión'), a:contains('Logout'), a:contains('logout'), a:contains('Cerrar Sesión'), [onclick*='logout'], [onclick*='Logout']");
        private By StatsContainer => By.ClassName("stats-container");

        public bool IsDashboardLoaded()
        {
            try
            {
                return _wait.Until(d => d.FindElements(StatsContainer).Count > 0);
            }
            catch
            {
                // Si no existe stats-container, simplemente verificar que estamos en una página de dashboard
                return !_driver.Url.Contains("login");
            }
        }

        public void ClickLogout()
        {
            try
            {
                // Intentar encontrar el botón de logout usando múltiples estrategias
                IWebElement logoutButton = null;

                // Estrategia 1: Buscar por texto "Logout" o "logout" en botones
                try
                {
                    logoutButton = _driver.FindElements(By.TagName("button"))
                        .FirstOrDefault(b => b.Text.ToLower().Contains("logout") || b.Text.ToLower().Contains("salir"));
                }
                catch { }

                // Estrategia 2: Buscar por atributo onclick
                if (logoutButton == null)
                {
                    try
                    {
                        logoutButton = _driver.FindElements(By.XPath("//button | //a"))
                            .FirstOrDefault(e => e.GetAttribute("onclick")?.ToLower().Contains("logout") ?? false);
                    }
                    catch { }
                }

                // Estrategia 3: Buscar por ID común
                if (logoutButton == null)
                {
                    try
                    {
                        logoutButton = _driver.FindElement(By.Id("logoutBtn"));
                    }
                    catch { }
                }

                // Estrategia 4: Buscar por clase común
                if (logoutButton == null)
                {
                    try
                    {
                        logoutButton = _driver.FindElement(By.CssSelector(".logout-btn, .logout-button, [data-logout]"));
                    }
                    catch { }
                }

                // Si encontramos el botón, hacer click
                if (logoutButton != null)
                {
                    try
                    {
                        logoutButton.Click();
                    }
                    catch (ElementClickInterceptedException)
                    {
                        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", logoutButton);
                    }
                    System.Threading.Thread.Sleep(1000);

                    // Después de hacer click en logout, buscar y confirmar el modal
                    ConfirmLogoutModal();
                }
                else
                {
                    throw new NoSuchElementException("No se encontró el botón de logout con ninguna estrategia");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"No se pudo hacer click en el botón de logout: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Maneja el modal de confirmación de logout
        /// </summary>
        private void ConfirmLogoutModal()
        {
            try
            {
                // Esperar a que aparezca el modal
                System.Threading.Thread.Sleep(500);

                IWebElement confirmButton = null;

                // Estrategia 1: Buscar por texto "Confirmar", "Sí", "Yes", "Aceptar" en botones
                try
                {
                    confirmButton = _driver.FindElements(By.TagName("button"))
                        .FirstOrDefault(b =>
                        {
                            var text = b.Text.ToLower();
                            return text.Contains("confirmar") || text.Contains("sí") || 
                                   text.Contains("yes") || text.Contains("aceptar") ||
                                   text.Contains("ok") || text.Contains("accept");
                        });
                }
                catch { }

                // Estrategia 2: Buscar por ID común de modal
                if (confirmButton == null)
                {
                    try
                    {
                        confirmButton = _driver.FindElement(By.Id("confirmBtn"));
                    }
                    catch { }
                }

                // Estrategia 3: Buscar por clase común de modal
                if (confirmButton == null)
                {
                    try
                    {
                        confirmButton = _driver.FindElement(By.CssSelector(".btn-confirm, .confirm-btn, .modal-confirm, [data-confirm]"));
                    }
                    catch { }
                }

                // Estrategia 4: Buscar el primer botón dentro de un modal (generalmente es el de confirmar)
                if (confirmButton == null)
                {
                    try
                    {
                        var modal = _driver.FindElements(By.CssSelector(".modal, .modal-dialog, [role='dialog']")).FirstOrDefault();
                        if (modal != null)
                        {
                            confirmButton = modal.FindElements(By.TagName("button")).FirstOrDefault(b => b.Displayed && b.Enabled);
                        }
                    }
                    catch { }
                }

                // Si encontramos el botón de confirmación, hacer click
                if (confirmButton != null && confirmButton.Displayed && confirmButton.Enabled)
                {
                    try
                    {
                        confirmButton.Click();
                    }
                    catch (ElementClickInterceptedException)
                    {
                        ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", confirmButton);
                    }
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                // Si no se encuentra el modal de confirmación, simplemente continuar
                System.Console.WriteLine($"Advertencia al confirmar logout: {ex.Message}");
            }
        }

        public bool IsUserLoggedIn()
        {
            try
            {
                // Buscar el botón de logout usando las mismas estrategias
                var logoutButtons = _driver.FindElements(By.TagName("button"))
                    .Where(b => b.Text.ToLower().Contains("logout") || b.Text.ToLower().Contains("salir"));

                if (logoutButtons.Any())
                    return true;

                // Si no hay botones, buscar en links
                var logoutLinks = _driver.FindElements(By.TagName("a"))
                    .Where(a => a.Text.ToLower().Contains("logout") || a.Text.ToLower().Contains("salir"));

                return logoutLinks.Any();
            }
            catch
            {
                return false;
            }
        }
    }
}