using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaDonacion.Models
{
    public class BitacoraAccion
    {
        public int Id { get; set; }

        [Required]
        public int UsuarioId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Accion { get; set; } = string.Empty; // Crear, Actualizar, Eliminar

        [Required]
        [MaxLength(100)]
        public string Tabla { get; set; } = string.Empty; // Donantes, Organos, Pacientes

        [Required]
        public int RegistroId { get; set; }

        public string? DatosAnteriores { get; set; }

        public string? DatosNuevos { get; set; }

        public string? DetallesCambios { get; set; }

        public string? IPAddress { get; set; }

        public DateTime FechaAccion { get; set; } = DateTime.Now;

        public string? Detalles { get; set; }

        // Relación con usuario
        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser? Usuario { get; set; }
    }
}
