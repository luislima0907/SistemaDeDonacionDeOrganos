using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace SistemaDonacion.Tests.E2E.Pages
{
    public class PacientesPage
    {
        private readonly IWebDriver _driver;
        private readonly WebDriverWait _wait;

        public PacientesPage(IWebDriver driver)
        {
            _driver = driver;
            _wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
        }

        // Localizadores para la tabla de pacientes
        private By PacientesTable => By.CssSelector("table tbody");
        private By PacientesRows => By.CssSelector("table tbody tr");
        private By Paciente_NombreInput => By.Id("nombre");
        private By Paciente_TipoSanguineoSelect => By.Id("tipoSanguineo");
        private By Paciente_OrganoRequeriodoSelect => By.Id("organoRequerido");
        private By Paciente_NivelUrgenciaSelect => By.Id("nivelUrgencia");
        private By Paciente_HospitalSelect => By.Id("hospitalId");
        private By Paciente_ObservacionesInput => By.Id("observaciones");
        private By FormSubmitButton => By.CssSelector("button[type='submit']");
        private By AgregarPacienteTabButton => By.CssSelector("button.tab-btn[onclick*='registrar-paciente']");
        private By ErrorMessage => By.CssSelector("#message .alert-error");
        private By SuccessMessage => By.CssSelector("#message .alert-success");
        private By EditarButton => By.CssSelector("button[data-action='editar'], .btn-editar");
        private By EliminarButton => By.CssSelector("button[data-action='eliminar'], .btn-eliminar");
        private By VerDetallesButton => By.CssSelector("button[data-action='ver'], .btn-ver-detalles");

        // Navegación
        public void Navigate(string baseUrl)
        {
            _driver.Navigate().GoToUrl($"{baseUrl}/pacientes.html");
        }

        // Obtener lista de pacientes
        public int GetPacientesCount()
        {
            try
            {
                return _wait.Until(d => d.FindElements(PacientesRows).Count);
            }
            catch
            {
                return 0;
            }
        }

        public void IrAListarPacientes()
        {
            var tabBtn = _driver.FindElements(By.CssSelector("button.tab-btn"))
                .FirstOrDefault(b => b.Text.Contains("Listar"));
            tabBtn?.Click();
            System.Threading.Thread.Sleep(1000);
        }

        public bool IsPacientesTableVisible()
        {
            return _driver.FindElements(PacientesTable).Count > 0;
        }

        // Crear paciente
        private void WaitForHospitalOptionsLoaded()
        {
            _wait.Until(d => d.FindElements(Paciente_HospitalSelect).Count > 0);
            _wait.Until(d => d.FindElement(Paciente_HospitalSelect).FindElements(By.TagName("option")).Count > 1);
        }

        public void EnterNombre(string nombre)
        {
            _wait.Until(d => d.FindElement(Paciente_NombreInput)).Clear();
            _driver.FindElement(Paciente_NombreInput).SendKeys(nombre);
        }

        public void SelectTipoSanguineo(string tipo)
        {
            var select = new SelectElement(_driver.FindElement(Paciente_TipoSanguineoSelect));
            select.SelectByValue(tipo);
        }

        public void SelectOrganoRequerido(string organo)
        {
            var select = new SelectElement(_driver.FindElement(Paciente_OrganoRequeriodoSelect));
            select.SelectByValue(organo);
        }

        public void SelectNivelUrgencia(string nivel)
        {
            var select = new SelectElement(_driver.FindElement(Paciente_NivelUrgenciaSelect));
            select.SelectByValue(nivel);
        }

        public void SelectHospital()
        {
            var select = new SelectElement(_driver.FindElement(Paciente_HospitalSelect));
            if (select.Options.Count > 1)
            {
                select.SelectByIndex(1);
            }
        }

        public void EnterObservaciones(string observaciones)
        {
            var element = _driver.FindElement(Paciente_ObservacionesInput);
            element.Clear();
            element.SendKeys(observaciones);
        }

        public void ClickSubmit()
        {
            _driver.FindElement(FormSubmitButton).Click();
        }

        public void ClickAgregarPaciente()
        {
            var button = _driver.FindElements(AgregarPacienteTabButton).FirstOrDefault();
            if (button != null)
            {
                button.Click();
            }
        }

        public void CrearPaciente(string nombre, string tipoSanguineo, string organo, string urgencia, string estado = "Activo", string observaciones = "")
        {
            WaitForHospitalOptionsLoaded();
            EnterNombre(nombre);
            SelectTipoSanguineo(tipoSanguineo);
            SelectOrganoRequerido(organo);
            SelectNivelUrgencia(urgencia);
            SelectHospital();
            if (!string.IsNullOrEmpty(observaciones))
            {
                EnterObservaciones(observaciones);
            }
            ClickSubmit();
        }

        // Mensajes
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

        public string GetSuccessMessage()
        {
            try
            {
                return _wait.Until(d => d.FindElement(SuccessMessage)).Text;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Acciones en tabla
        public void ClickEditarPaciente(int rowIndex)
        {
            var buttons = _driver.FindElements(EditarButton);
            if (rowIndex < buttons.Count)
            {
                buttons[rowIndex].Click();
            }
        }

        public void ClickEliminarPaciente(int rowIndex)
        {
            var buttons = _driver.FindElements(EliminarButton);
            if (rowIndex < buttons.Count)
            {
                buttons[rowIndex].Click();
            }
        }

        public void ClickVerDetalles(int rowIndex)
        {
            var buttons = _driver.FindElements(VerDetallesButton);
            if (rowIndex < buttons.Count)
            {
                buttons[rowIndex].Click();
            }
        }

        // Verificar contenido en tabla
        public bool PacienteExisteEnTabla(string nombre)
        {
            try
            {
                var rows = _wait.Until(d => d.FindElements(PacientesRows));
                foreach (var row in rows)
                {
                    if (row.Text.Contains(nombre))
                        return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Esperar a que desaparezca el modal de carga
        public void WaitForModalToClose()
        {
            System.Threading.Thread.Sleep(2000);
        }
    }
}
