using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaDonacion.Models
{
    public class Organo
    {
        public int Id { get; set; }

        [Required]
        public int DonanteId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TipoOrgano { get; set; } = string.Empty; // Corazón, Pulmón, Hígado, Riñón, Páncreas, Córnea

        [MaxLength(50)]
        public string Estado { get; set; } = "Disponible"; // Disponible, Asignado, Descartado, Trasplantado

        public DateTime FechaDisponibilidad { get; set; } = DateTime.Now;

        public string? Compatibilidad { get; set; } // JSON con información de compatibilidad

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // Relación con donante
        [ForeignKey("DonanteId")]
        public virtual Donante? Donante { get; set; }
    }
}