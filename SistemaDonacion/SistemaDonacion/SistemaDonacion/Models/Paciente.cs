using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaDonacion.Models
{
    public class Paciente
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string TipoSanguineo { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string OrganoRequerido { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string NivelUrgencia { get; set; } = string.Empty; // Alta, Media, Baja

        [Required]
        public int HospitalId { get; set; }

        [MaxLength(50)]
        public string Estado { get; set; } = "Activo";

        public string? Observaciones { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;
        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        [ForeignKey("HospitalId")]
        public virtual Hospital? Hospital { get; set; }
    }
}