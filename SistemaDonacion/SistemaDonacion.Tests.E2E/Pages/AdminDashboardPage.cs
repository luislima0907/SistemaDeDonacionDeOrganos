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

        private By DashboardTitle => By.CssSelector("h1");
        private By LogoutButton => By.CssSelector("button[onclick*='logout']");
        private By StatsContainer => By.ClassName("stats-container");

        public bool IsDashboardLoaded()
        {
            return _wait.Until(d => d.FindElements(StatsContainer).Count > 0);
        }

        public void ClickLogout()
        {
            _driver.FindElement(LogoutButton).Click();
        }

        public bool IsUserLoggedIn()
        {
            return _driver.FindElements(LogoutButton).Count > 0;
        }
    }
}