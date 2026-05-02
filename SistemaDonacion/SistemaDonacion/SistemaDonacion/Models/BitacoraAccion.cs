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
        public string Tabla { get; set; } = string.Empty; // Donantes, Organos, etc.

        [Required]
        public int RegistroId { get; set; }

        public string? DatosAnteriores { get; set; }

        public string? DatosNuevos { get; set; }

        public string? Detalles { get; set; }

        //  AGREGADOS (estos faltaban y causaban el error)
        public string? IPAddress { get; set; }

        public string? DetallesCambios { get; set; }

        public DateTime FechaAccion { get; set; } = DateTime.Now;

        // Relación con usuario
        [ForeignKey("UsuarioId")]
        public virtual ApplicationUser? Usuario { get; set; }
    }
}