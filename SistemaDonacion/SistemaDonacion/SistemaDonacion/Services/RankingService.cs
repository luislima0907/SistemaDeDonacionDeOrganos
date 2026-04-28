using SistemaDonacion.Data;
using SistemaDonacion.Models;
using Microsoft.EntityFrameworkCore;

namespace SistemaDonacion.Services
{
    public interface IRankingService
    {
        Task<List<RankingPrioridad>> ObtenerRankingPrioridadAsync(int organoId);
        Task<List<RankingPrioridad>> ObtenerRankingPrioridadPorTipoOrganoAsync(string tipoOrgano, string tipoSanguineoDonanteAsync);
    }

    public class RankingService : IRankingService
    {
        private readonly AppDbContext _context;
        private const int DIAS_ANTIGUEDAD_MAXIMA = 30;

        public RankingService(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Obtiene el ranking de prioridad para un órgano específico
        /// </summary>
        public async Task<List<RankingPrioridad>> ObtenerRankingPrioridadAsync(int organoId)
        {
            try
            {
                var organo = await _context.Organos
                    .Include(o => o.Donante)
                    .FirstOrDefaultAsync(o => o.Id == organoId);

                if (organo == null)
                    return new List<RankingPrioridad>();

                // Validar que el donante existe y tiene tipo de sangre
                if (organo.Donante == null || string.IsNullOrWhiteSpace(organo.Donante.TipoSanguineo))
                    return new List<RankingPrioridad>();

                return await ObtenerRankingPrioridadPorTipoOrganoAsync(organo.TipoOrgano, organo.Donante.TipoSanguineo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR RankingService.ObtenerRankingPrioridadAsync] {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new List<RankingPrioridad>();
            }
        }

        /// <summary>
        /// Obtiene el ranking de prioridad según el tipo de órgano y tipo de sangre del donante
        /// </summary>
        public async Task<List<RankingPrioridad>> ObtenerRankingPrioridadPorTipoOrganoAsync(string tipoOrgano, string tipoSanguineoDonante)
        {
            try
            {
                // Validar parámetros
                if (string.IsNullOrWhiteSpace(tipoOrgano) || string.IsNullOrWhiteSpace(tipoSanguineoDonante))
                    return new List<RankingPrioridad>();

                // Normalizar valores (trim y verificar que no sean vacíos después)
                tipoOrgano = tipoOrgano?.Trim() ?? "";
                tipoSanguineoDonante = tipoSanguineoDonante?.Trim() ?? "";

                if (string.IsNullOrWhiteSpace(tipoOrgano) || string.IsNullOrWhiteSpace(tipoSanguineoDonante))
                    return new List<RankingPrioridad>();

                // Obtener todos los pacientes
                var todosPacientes = await _context.Pacientes.ToListAsync();

                // Filtrar en memoria para tener más control
                var pacientesCompatibles = new List<Paciente>();

                foreach (var paciente in todosPacientes)
                {
                    // Validar que el paciente cumpla con los criterios
                    if (paciente == null)
                        continue;

                    // Filtro 1: Estado debe ser "Activo"
                    if (paciente.Estado != "Activo")
                        continue;

                    // Filtro 2: Órgano requerido debe coincidir
                    var organoRequerido = paciente.OrganoRequerido?.Trim() ?? "";
                    if (organoRequerido != tipoOrgano)
                        continue;

                    // Filtro 3: Tipo de sangre compatible
                    var tipoSanguineoPaciente = paciente.TipoSanguineo?.Trim() ?? "";
                    if (!EsCompatibleSanguineo(tipoSanguineoPaciente, tipoSanguineoDonante))
                        continue;

                    // Filtro 4: Datos no tan antiguos (pero menos restrictivo)
                    // Usamos FechaRegistro como referencia, no FechaActualizacion
                    var ahora = DateTime.Now;
                    var fechaLimite = ahora.AddDays(-DIAS_ANTIGUEDAD_MAXIMA);
                    
                    // Aceptar pacientes registrados recientemente O con actualización reciente
                    if (paciente.FechaRegistro < fechaLimite && paciente.FechaActualizacion < fechaLimite)
                        continue;

                    pacientesCompatibles.Add(paciente);
                }

                // Calcular puntaje y crear ranking
                var ranking = new List<RankingPrioridad>();

                foreach (var paciente in pacientesCompatibles)
                {
                    var puntaje = CalcularPuntajePrioridad(paciente);

                    ranking.Add(new RankingPrioridad
                    {
                        Posicion = 0, // Se actualizará después
                        PacienteId = paciente.Id,
                        NombrePaciente = paciente.Nombre,
                        TipoSanguineo = paciente.TipoSanguineo,
                        OrganoRequerido = paciente.OrganoRequerido,
                        NivelUrgencia = paciente.NivelUrgencia,
                        PuntajeTotal = puntaje,
                        Estado = paciente.Estado,
                        FechaActualizacion = paciente.FechaActualizacion,
                        CompatibilidadVerificada = true
                    });
                }

                // Ordenar por puntaje (descendente)
                ranking = ranking.OrderByDescending(r => r.PuntajeTotal)
                    .ThenBy(r => r.PacienteId) // Desempate por ID
                    .Select((r, index) => new RankingPrioridad
                    {
                        Posicion = index + 1,
                        PacienteId = r.PacienteId,
                        NombrePaciente = r.NombrePaciente,
                        TipoSanguineo = r.TipoSanguineo,
                        OrganoRequerido = r.OrganoRequerido,
                        NivelUrgencia = r.NivelUrgencia,
                        PuntajeTotal = r.PuntajeTotal,
                        Estado = r.Estado,
                        FechaActualizacion = r.FechaActualizacion,
                        CompatibilidadVerificada = r.CompatibilidadVerificada
                    })
                    .ToList();

                return ranking;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR RankingService.ObtenerRankingPrioridadPorTipoOrganoAsync] {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return new List<RankingPrioridad>();
            }
        }

        /// <summary>
        /// Calcula el puntaje de prioridad basado en criterios médicos
        /// Fórmula: Urgencia (40%) + Tiempo en espera (30%) + Compatibilidad (20%) + Edad (10%)
        /// </summary>
        private double CalcularPuntajePrioridad(Paciente paciente)
        {
            // Puntaje por nivel de urgencia (0-40 puntos)
            double puntajeUrgencia = ObtenerPuntajeUrgencia(paciente.NivelUrgencia);

            // Puntaje por tiempo en espera (0-30 puntos)
            double puntajeTiempoEspera = CalcularPuntajeTiempoEspera(paciente.FechaRegistro);

            // Puntaje por compatibilidad sanguínea (0-20 puntos)
            double puntajeCompatibilidad = 20.0; // Ya está filtrado, entonces tiene compatibilidad completa

            // Puntaje por edad (0-10 puntos) - favorece edades más jóvenes
            double puntajeEdad = 10.0; // Valor por defecto sin información de edad en el modelo

            // Puntaje total (máximo 100)
            double puntajeTotal = puntajeUrgencia + puntajeTiempoEspera + puntajeCompatibilidad + puntajeEdad;

            // Asegurar que el puntaje no sea negativo
            return Math.Max(0, puntajeTotal);
        }

        /// <summary>
        /// Obtiene el puntaje según el nivel de urgencia
        /// </summary>
        private double ObtenerPuntajeUrgencia(string nivelUrgencia)
        {
            return nivelUrgencia?.ToLower() switch
            {
                "alta" => 40.0,
                "media" => 20.0,
                "baja" => 10.0,
                _ => 0.0
            };
        }

        /// <summary>
        /// Calcula el puntaje según el tiempo en espera
        /// Máximo 30 puntos después de 365 días en espera
        /// </summary>
        private double CalcularPuntajeTiempoEspera(DateTime fechaRegistro)
        {
            var diasEnEspera = (DateTime.Now - fechaRegistro).TotalDays;
            var puntajeTiempo = Math.Min(30.0, (diasEnEspera / 365.0) * 30.0);
            return puntajeTiempo;
        }

        /// <summary>
        /// Verifica si el tipo de sangre del paciente es compatible con la del donante
        /// Matriz de compatibilidad: el receptor puede recibir de donantes con tipos de sangre compatibles
        /// </summary>
        private bool EsCompatibleSanguineo(string tipoReceptor, string tipoDonanteAsync)
        {
            // Matriz de compatibilidad: receptor -> donantes compatibles
            var compatibilidad = new Dictionary<string, List<string>>
            {
                { "O+", new List<string> { "O+", "O-" } },
                { "O-", new List<string> { "O-" } },
                { "A+", new List<string> { "A+", "A-", "O+", "O-" } },
                { "A-", new List<string> { "A-", "O-" } },
                { "B+", new List<string> { "B+", "B-", "O+", "O-" } },
                { "B-", new List<string> { "B-", "O-" } },
                { "AB+", new List<string> { "AB+", "AB-", "A+", "A-", "B+", "B-", "O+", "O-" } },
                { "AB-", new List<string> { "AB-", "A-", "B-", "O-" } }
            };

            if (!compatibilidad.TryGetValue(tipoReceptor, out var donantesCompatibles))
                return false;

            return donantesCompatibles.Contains(tipoDonanteAsync);
        }
    }
}
