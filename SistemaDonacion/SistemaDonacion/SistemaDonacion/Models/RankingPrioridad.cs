using System.ComponentModel.DataAnnotations;

namespace SistemaDonacion.Models
{
    public class RankingPrioridad
    {
        public int Posicion { get; set; }
        public int PacienteId { get; set; }
        public string NombrePaciente { get; set; } = string.Empty;
        public string TipoSanguineo { get; set; } = string.Empty;
        public string OrganoRequerido { get; set; } = string.Empty;
        public string NivelUrgencia { get; set; } = string.Empty;
        public double PuntajeTotal { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaActualizacion { get; set; }
        public bool CompatibilidadVerificada { get; set; }
    }
}

