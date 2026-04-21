using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SistemaDonacion.Models
{
    public class Hospital
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Ciudad { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Pais { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Telefono { get; set; }

        [MaxLength(256)]
        public string? Email { get; set; }

        public bool Estado { get; set; } = true;

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        // Relación con donantes - ignorar en serialización para evitar ciclos
        [JsonIgnore]
        public virtual ICollection<Donante> Donantes { get; set; } = new List<Donante>();
    }
}
