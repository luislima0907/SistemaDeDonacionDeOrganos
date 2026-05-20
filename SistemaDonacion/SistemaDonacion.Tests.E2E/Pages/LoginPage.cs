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
        private By LoginButton => By.CssSelector("button[type='submit']");
        private By ErrorMessage => By.CssSelector(".error-message");

        public void Navigate(string baseUrl)
        {
            _driver.Navigate().GoToUrl($"{baseUrl}/login");
        }

        public void EnterUsername(string username)
        {
            _wait.Until(d => d.FindElement(UsernameInput)).SendKeys(username);
        }

        public void EnterPassword(string password)
        {
            _driver.FindElement(PasswordInput).SendKeys(password);
        }

        public void ClickLogin()
        {
            _driver.FindElement(LoginButton).Click();
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
                return _wait.Until(d => d.FindElement(ErrorMessage)).Text;
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool IsLoginFormVisible()
        {
            return _driver.FindElements(UsernameInput).Count > 0;
        }
    }
}