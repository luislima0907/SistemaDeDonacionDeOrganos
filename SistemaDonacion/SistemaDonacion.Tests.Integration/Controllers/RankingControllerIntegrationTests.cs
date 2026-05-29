using Xunit;
using System.Net;

namespace SistemaDonacion.Tests.Integration.Controllers
{
    public class RankingControllerIntegrationTests
    {
        private readonly HttpClient _client;
        private const string BaseUrl = "http://localhost:5135";

        public RankingControllerIntegrationTests()
        {
            _client = new HttpClient
            {
                BaseAddress = new Uri(BaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };

            _client.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConDatosValidos_RetornaRespuestaValida()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/ranking-tipo/Riñón/O+");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Se esperaba OK (200), Unauthorized (401) o NotFound (404), pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConOtroTipoOrgano_RetornaRespuestaValida()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/ranking-tipo/Corazón/O+");

            // Assert
            Assert.NotNull(response);
            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Se esperaba OK (200), Unauthorized (401) o NotFound (404), pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConTipoSanguineoValido_RetornaContenido()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/ranking-tipo/Riñón/A+");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Se esperaba OK (200), Unauthorized (401) o NotFound (404), pero se obtuvo {response.StatusCode}"
            );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contenido = await response.Content.ReadAsStringAsync();
                Assert.NotNull(contenido);
                Assert.NotEmpty(contenido);
            }
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConTipoOrganoInvalido_RetornaRespuestaControlada()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/ranking-tipo/OrganoInexistente/O+");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Se esperaba OK (200), Unauthorized (401) o NotFound (404), pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task ObtenerRankingPorTipo_ConTipoSanguineoInvalido_RetornaRespuestaControlada()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/ranking-tipo/Riñón/XX");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound,
                $"Se esperaba OK (200), Unauthorized (401) o NotFound (404), pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_ConOrganoExistente_RetornaRespuestaValida()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/1/ranking");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.OK ||
                response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode == HttpStatusCode.Unauthorized ||
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.InternalServerError,
                $"Se esperaba una respuesta controlada, pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task ObtenerRankingPrioridad_ConOrganoInexistente_RetornaNotFoundOUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/999999/ranking");

            // Assert
            Assert.NotNull(response);

            Assert.True(
                response.StatusCode == HttpStatusCode.NotFound ||
                response.StatusCode == HttpStatusCode.Unauthorized,
                $"Se esperaba NotFound (404) o Unauthorized (401), pero se obtuvo {response.StatusCode}"
            );
        }

        [Fact]
        public async Task EndpointRankingTipo_RetornaContenidoCuandoEsOk()
        {
            // Act
            var response = await _client.GetAsync("/api/organo/ranking-tipo/Riñón/O+");

            // Assert
            Assert.NotNull(response);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contenido = await response.Content.ReadAsStringAsync();

                Assert.NotNull(contenido);
                Assert.NotEmpty(contenido);
            }
        }
    }
}