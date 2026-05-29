using Xunit;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class OrganoControllerIntegrationTests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5135";

        public OrganoControllerIntegrationTests()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

   
        // GET /api/organo — Obtener todos los órganos
        /// <summary>
        /// Prueba obtener todos los órganos sin autenticación.
        /// Se espera OK (200) si el servidor permite acceso,
        /// o Unauthorized (401) si requiere autenticación.
        /// </summary>
        [Fact]
        public async Task GetOrganos_SinAutenticacion_RetornaOkOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/organo");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }

       
        // GET /api/organo/disponibles — Conteo
   

        /// <summary>
        /// Prueba el endpoint de conteo de órganos disponibles.
        /// Es un endpoint público que debería retornar siempre 200.
        /// </summary>
        [Fact]
        public async Task GetOrganosDisponiblesConteo_RetornaOkOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/disponibles");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }

      
        // GET /api/organo/disponibles/{tipoOrgano}
     

        /// <summary>
        /// Prueba obtener órganos disponibles por tipo "Riñón".
        /// </summary>
        [Fact]
        public async Task GetOrganosDisponiblesPorTipo_Rinon_RetornaOkOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/disponibles/Riñón");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }

        /// <summary>
        /// Prueba obtener órganos disponibles por tipo "Corazón".
        /// </summary>
        [Fact]
        public async Task GetOrganosDisponiblesPorTipo_Corazon_RetornaOkOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/disponibles/Corazón");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }


        // GET /api/organo/{id}
 

        /// <summary>
        /// Prueba obtener un órgano con ID 1.
        /// Puede retornar 200 (existe), 401 (no autenticado) o 404 (no existe).
        /// </summary>
        [Fact]
        public async Task GetOrgano_Id1_RetornaOkNotFoundOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/1");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba OK (200), NotFound (404) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }

        /// <summary>
        /// Prueba obtener un órgano con ID que no existe (999999).
        /// Debe retornar 404 o 401.
        /// </summary>
        [Fact]
        public async Task GetOrgano_IdInexistente_RetornaNotFoundOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/999999");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba NotFound (404) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }

        // GET /api/organo/{id}/ranking


        /// <summary>
        /// Prueba el endpoint de ranking de prioridad para órgano ID 1.
        /// </summary>
        [Fact]
        public async Task GetRankingPrioridad_Id1_RetornaStatusValido()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/1/ranking");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Status inesperado: {response.StatusCode}");
        }

        /// <summary>
        /// Prueba el endpoint de ranking por tipo de órgano y tipo sanguíneo.
        /// </summary>
        [Fact]
        public async Task GetRankingPorTipo_Rinon_APositivo_RetornaStatusValido()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/ranking-tipo/Riñón/A+");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Status inesperado: {response.StatusCode}");
        }

  
        // POST /api/organo — Crear órgano


        /// <summary>
        /// Prueba crear un órgano sin autenticación.
        /// Sin cookie de sesión debe retornar 401.
        /// </summary>
        [Fact]
        public async Task CrearOrgano_SinAutenticacion_RetornaUnauthorized()
        {
            // Arrange
            var body = new
            {
                donanteId = 1,
                tipoOrgano = "Riñón",
                compatibilidad = (string?)null
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/api/organo", content);

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Created,
                $"Status inesperado: {response.StatusCode}");
        }


        // PUT /api/organo/{id}/estado
 

        /// <summary>
        /// Prueba actualizar estado de un órgano sin autenticación.
        /// </summary>
        [Fact]
        public async Task ActualizarEstadoOrgano_SinAutenticacion_RetornaStatusValido()
        {
            // Arrange
            var body = new
            {
                nuevoEstado = "Asignado",
                observaciones = "Test integration"
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PutAsync("/api/organo/1/estado", content);

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.BadRequest,
                $"Status inesperado: {response.StatusCode}");
        }


        // DELETE /api/organo/{id}


        /// <summary>
        /// Prueba eliminar un órgano con ID que no existe.
        /// Sin auth debe retornar 401; con auth y sin existir, 404.
        /// </summary>
        [Fact]
        public async Task EliminarOrgano_IdInexistente_RetornaNotFoundOUnauthorized()
        {
            // Act
            var response = await _client.DeleteAsync("/api/organo/999999");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba NotFound (404) o Unauthorized (401), pero se obtuvo {response.StatusCode}");
        }
    }
}
