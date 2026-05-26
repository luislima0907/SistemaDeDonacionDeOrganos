using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SistemaDonacion.Tests.E2E.Pages
{
    public class LoginPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public LoginPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // Localizadores
        private By UsernameInput => By.Id("username");
        private By PasswordInput => By.Id("password");
        private By LoginButton => By.Id("submitBtn");
        private By ErrorMessage => By.CssSelector(".error-message");

        public void Navigate(string baseUrl)
        {
            _driver.Navigate().GoToUrl(baseUrl);
            System.Threading.Thread.Sleep(1000);
        }

        public void EnterUsername(string username)
        {
            var element = _wait.Until(d => d.FindElement(UsernameInput));
            element.Clear();
            element.SendKeys(username);
            System.Threading.Thread.Sleep(500);
        }

        public void EnterPassword(string password)
        {
            var element = _wait.Until(d => d.FindElement(PasswordInput));
            element.Clear();
            element.SendKeys(password);
            System.Threading.Thread.Sleep(500);
        }

        public void ClickLogin()
        {
            try
            {
                // Esperar a que el elemento sea clickeable
                var button = _wait.Until(d =>
                {
                    var elem = d.FindElement(LoginButton);
                    if (elem.Displayed && elem.Enabled)
                        return elem;
                    return null;
                });

                // Usar JavaScript para hacer el click si el click normal falla
                try
                {
                    button.Click();
                }
                catch (ElementClickInterceptedException)
                {
                    ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", button);
                }

                System.Threading.Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"No se pudo hacer click en el botón de login: {ex.Message}", ex);
            }
        }

        public void Login(string username, string password)
        {
            EnterUsername(username);
            EnterPassword(password);
            ClickLogin();
        }

        public string GetErrorMessage()
        {
            try
            {
                // Esperar a que aparezca el mensaje de error
                System.Threading.Thread.Sleep(500);

                string errorText = string.Empty;

                // Estrategia 1: Buscar por clase .error-message
                try
                {
                    var errorElement = _wait.Until(d => 
                    {
                        var elem = d.FindElements(By.CssSelector(".error-message")).FirstOrDefault();
                        if (elem != null && elem.Displayed)
                            return elem;
                        return null;
                    });
                    if (errorElement != null)
                        errorText = errorElement.Text;
                }
                catch { }

                // Estrategia 2: Buscar por clase alert (Bootstrap)
                if (string.IsNullOrEmpty(errorText))
                {
                    try
                    {
                        var alertElement = _driver.FindElements(By.CssSelector(".alert, .alert-danger, .alert-warning, .alert-error"))
                            .FirstOrDefault(a => a.Displayed && !string.IsNullOrEmpty(a.Text));
                        if (alertElement != null)
                            errorText = alertElement.Text;
                    }
                    catch { }
                }

                // Estrategia 3: Buscar por atributo role="alert" (Bootstrap accessibility)
                if (string.IsNullOrEmpty(errorText))
                {
                    try
                    {
                        var alertElement = _driver.FindElements(By.CssSelector("[role='alert']"))
                            .FirstOrDefault(a => a.Displayed && !string.IsNullOrEmpty(a.Text));
                        if (alertElement != null)
                            errorText = alertElement.Text;
                    }
                    catch { }
                }

                // Estrategia 4: Buscar por class que contenga "error" o "invalid"
                if (string.IsNullOrEmpty(errorText))
                {
                    try
                    {
                        var errorElements = _driver.FindElements(By.XPath("//*[contains(@class, 'error') or contains(@class, 'invalid') or contains(@class, 'danger')]"))
                            .Where(e => e.Displayed && !string.IsNullOrEmpty(e.Text) && e.Text.Length > 3)
                            .ToList();
                        if (errorElements.Any())
                            errorText = errorElements.First().Text;
                    }
                    catch { }
                }

                // Estrategia 5: Buscar mensaje de error debajo del campo de contraseña
                if (string.IsNullOrEmpty(errorText))
                {
                    try
                    {
                        var passwordInput = _driver.FindElement(By.Id("password"));
                        var parent = (IWebElement)((IJavaScriptExecutor)_driver).ExecuteScript("return arguments[0].parentElement;", passwordInput);
                        var errorSpan = parent.FindElements(By.CssSelector("small, .form-text, .invalid-feedback"))
                            .FirstOrDefault(e => e.Displayed && !string.IsNullOrEmpty(e.Text));
                        if (errorSpan != null)
                            errorText = errorSpan.Text;
                    }
                    catch { }
                }

                // Estrategia 6: Buscar cualquier elemento con texto que parezca un error
                if (string.IsNullOrEmpty(errorText))
                {
                    try
                    {
                        var allText = _driver.FindElements(By.XPath("//*[contains(text(), 'inválid') or contains(text(), 'incorrecto') or contains(text(), 'error') or contains(text(), 'incorrecta')]"))
                            .FirstOrDefault(e => e.Displayed && !string.IsNullOrEmpty(e.Text));
                        if (allText != null)
                            errorText = allText.Text;
                    }
                    catch { }
                }

                return errorText;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error al obtener mensaje de error: {ex.Message}");
                return string.Empty;
            }
        }

        public bool IsLoginFormVisible()
        {
            return _driver.FindElements(UsernameInput).Count > 0;
        }
    }
}