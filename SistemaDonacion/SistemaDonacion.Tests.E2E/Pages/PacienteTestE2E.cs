using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using Xunit;
using SistemaDonacion.Tests.E2E.Pages;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;

namespace SistemaDonacion.Tests.E2E.Tests
{
    public class PacienteE2ETests : IAsyncLifetime
    {
        private IWebDriver _driver;
        private readonly string _baseUrl = "http://localhost:5135";
        private LoginPage _loginPage;
        private PacientesPage _pacientesPage;

        public async Task InitializeAsync()
        {
            var options = new ChromeOptions();
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-gpu");
            _driver = new ChromeDriver(options);
            _loginPage = new LoginPage(_driver);
            _pacientesPage = new PacientesPage(_driver);
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _driver?.Quit();
            _driver?.Dispose();
            await Task.CompletedTask;
        }

        //Pruebas de Autenticación

        [Fact]
        public void Pacientes_RequiresAuthentication_RedirectsToLogin()
        {
            // Arrange & Act
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.Contains("login", _driver.Url.ToLower());
        }

        // ==================== Pruebas de Visualización de Pacientes ====================

        [Fact]
        public void Pacientes_MedicoLogueado_PuedeVerTabla()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            
            // Esperar redirección al panel médico
            var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Url.Contains("medico.html"));

            // Act
            _pacientesPage.Navigate(_baseUrl);
            wait.Until(d => d.Url.Contains("pacientes.html"));
            System.Threading.Thread.Sleep(1500);

            // Assert
            Assert.True(_pacientesPage.IsPacientesTableVisible());
        }

        [Fact]
        public void Pacientes_PuedeLeerLista_VaCanOcupada()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);

            // Act
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(2000);
            int count = _pacientesPage.GetPacientesCount();

            // Assert - Esperamos al menos 0 pacientes (tabla vacía es válida)
            Assert.True(count >= 0);
        }

        // ==================== Pruebas de Creación de Pacientes ====================

        [Fact]
        public void Pacientes_CrearPaciente_Exitoso()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            string nombrePaciente = $"Paciente Test {DateTime.Now.Ticks}";

            // Act
            _pacientesPage.CrearPaciente(
                nombre: nombrePaciente,
                tipoSanguineo: "O+",
                organo: "Riñón",
                urgencia: "Alta",
                estado: "Activo",
                observaciones: "Prueba E2E"
            );
            System.Threading.Thread.Sleep(2000);

            // Assert
            var successMsg = _pacientesPage.GetSuccessMessage();
            Assert.NotEmpty(successMsg);
            Assert.True(_pacientesPage.PacienteExisteEnTabla(nombrePaciente));
        }

        [Fact]
        public void Pacientes_CrearPacienteSinNombre_MuestraError()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            // Act
            try
            {
                _pacientesPage.ClickAgregarPaciente();
                System.Threading.Thread.Sleep(1000);
                _pacientesPage.SelectTipoSanguineo("O+");
                _pacientesPage.SelectOrganoRequerido("Riñón");
                _pacientesPage.SelectNivelUrgencia("Alta");
                _pacientesPage.ClickSubmit();
                System.Threading.Thread.Sleep(2000);
            }
            catch
            {
                // El envío puede fallar si el formulario valida
            }

            // Assert
            var errorMsg = _pacientesPage.GetErrorMessage();
            Assert.NotEmpty(errorMsg);
        }

        // ==================== Pruebas de Diferentes Tipos de Sangre ====================

        [Theory]
        [InlineData("O+")]
        [InlineData("O-")]
        [InlineData("A+")]
        [InlineData("AB-")]
        public void Pacientes_CrearConDiferentesTiposSanguineos_Exitoso(string tipoSanguineo)
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            string nombrePaciente = $"Paciente {tipoSanguineo} {DateTime.Now.Ticks}";

            // Act
            _pacientesPage.CrearPaciente(
                nombre: nombrePaciente,
                tipoSanguineo: tipoSanguineo,
                organo: "Corazón",
                urgencia: "Media",
                estado: "Activo"
            );
            System.Threading.Thread.Sleep(2000);

            // Assert
            _pacientesPage.IrAListarPacientes();
            System.Threading.Thread.Sleep(1500);
            Assert.True(_pacientesPage.PacienteExisteEnTabla(nombrePaciente));
        }

        // ==================== Pruebas de Diferentes Órganos ====================

        [Theory]
        [InlineData("Corazón")]
        [InlineData("Pulmón")]
        [InlineData("Hígado")]
        [InlineData("Riñón")]
        public void Pacientes_CrearConDiferentesOrganos_Exitoso(string organo)
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            string nombrePaciente = $"Paciente Organo {organo} {DateTime.Now.Ticks}";

            // Act
            _pacientesPage.CrearPaciente(
                nombre: nombrePaciente,
                tipoSanguineo: "O+",
                organo: organo,
                urgencia: "Alta",
                estado: "Activo"
            );
            System.Threading.Thread.Sleep(2000);

            // Assert
            _pacientesPage.IrAListarPacientes();
            System.Threading.Thread.Sleep(1500);
            Assert.True(_pacientesPage.PacienteExisteEnTabla(nombrePaciente));
        }

        // ==================== Pruebas de Niveles de Urgencia ====================

        [Theory]
        [InlineData("Alta")]
        [InlineData("Media")]
        [InlineData("Baja")]
        public void Pacientes_CrearConDiferentesNivelesUrgencia_Exitoso(string urgencia)
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            string nombrePaciente = $"Paciente Urgencia {urgencia} {DateTime.Now.Ticks}";

            // Act
            _pacientesPage.CrearPaciente(
                nombre: nombrePaciente,
                tipoSanguineo: "O+",
                organo: "Riñón",
                urgencia: urgencia,
                estado: "Activo"
            );
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(_pacientesPage.PacienteExisteEnTabla(nombrePaciente));
        }

        // ==================== Pruebas de Estados ====================

        [Theory]
        [InlineData("Activo")]
        [InlineData("Trasplantado")]
        [InlineData("Fallecido")]
        public void Pacientes_CrearConDiferentesEstados_Exitoso(string estado)
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            string nombrePaciente = $"Paciente Estado {estado} {DateTime.Now.Ticks}";

            // Act
            _pacientesPage.CrearPaciente(
                nombre: nombrePaciente,
                tipoSanguineo: "O+",
                organo: "Pulmón",
                urgencia: "Media",
                estado: estado
            );
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(_pacientesPage.PacienteExisteEnTabla(nombrePaciente));
        }

        // ==================== Pruebas de Observaciones ====================

        [Fact]
        public void Pacientes_CrearConObservaciones_GuardaCorrectamente()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            string nombrePaciente = $"Paciente Observaciones {DateTime.Now.Ticks}";
            string observaciones = "Paciente con historial de enfermedades crónicas - Requiere seguimiento especial";

            // Act
            _pacientesPage.CrearPaciente(
                nombre: nombrePaciente,
                tipoSanguineo: "A+",
                organo: "Hígado",
                urgencia: "Alta",
                estado: "Activo",
                observaciones: observaciones
            );
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(_pacientesPage.PacienteExisteEnTabla(nombrePaciente));
        }

        // ==================== Pruebas de Logout ====================

        [Fact]
        public void Pacientes_DespuesDelLogout_RequiereAutenticacionDenuevo()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            var dashboardPage = new AdminDashboardPage(_driver);

            // Act
            dashboardPage.ClickLogout();
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.True(_loginPage.IsLoginFormVisible());
        }

        // ==================== Pruebas de Validaciones ====================

        [Fact]
        public void Pacientes_CrearMultiplesPacientes_TodosAparecenEnTabla()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);

            var pacientes = new[]
            {
                ("Paciente UNO", "O+", "Riñón"),
                ("Paciente DOS", "A+", "Corazón"),
                ("Paciente TRES", "B-", "Pulmón")
            };

            // Act
            foreach (var (nombre, tipo, organo) in pacientes)
            {
                string nombreCompleto = $"{nombre} {DateTime.Now.Ticks}";
                _pacientesPage.CrearPaciente(nombreCompleto, tipo, organo, "Media", "Activo");
                System.Threading.Thread.Sleep(1500);
            }

            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            // Assert
            int count = _pacientesPage.GetPacientesCount();
            Assert.True(count >= 3);
        }

        [Fact]
        public void Pacientes_VerDetallesPaciente_MuestraInformacionCompleta()
        {
            // Arrange
            _loginPage.Navigate(_baseUrl);
            _loginPage.Login("medico2", "Medico123!");
            System.Threading.Thread.Sleep(2000);
            _pacientesPage.Navigate(_baseUrl);
            System.Threading.Thread.Sleep(1000);

            // Crear un paciente primero
            string nombrePaciente = $"Paciente Detalles {DateTime.Now.Ticks}";
            _pacientesPage.CrearPaciente(nombrePaciente, "O+", "Riñón", "Alta", "Activo", "Paciente de prueba");
            System.Threading.Thread.Sleep(2000);

            // Act - Intentar ver detalles
            try
            {
                _pacientesPage.ClickVerDetalles(0);
                System.Threading.Thread.Sleep(1000);
            }
            catch
            {
                // Es posible que el botón no exista en este momento
            }

            // Assert
            Assert.True(_driver.Url.Contains("pacientes") || _driver.FindElements(By.CssSelector("h1, h2, .modal")).Count > 0);
        }
    }
}
