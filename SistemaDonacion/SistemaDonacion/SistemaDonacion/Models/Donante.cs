using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace SistemaDonacion.Models
{
    public class Donante
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(10)]
        public string TipoSanguineo { get; set; } = string.Empty;

        [Required]
        public int Edad { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [MaxLength(50)]
        public string Estado { get; set; } = "Disponible"; // Disponible, Asignado, Rechazado

        [Required]
        public int HospitalId { get; set; }

        public string? Observaciones { get; set; }

        public DateTime FechaActualizacion { get; set; } = DateTime.Now;

        // Relación con hospital
        [ForeignKey("HospitalId")]
        public virtual Hospital? Hospital { get; set; }

        // Relación con órganos - ignorar en serialización para evitar ciclos
        [JsonIgnore]
        public virtual ICollection<Organo> Organos { get; set; } = new List<Organo>();
    }
}
