using System.ComponentModel.DataAnnotations;

namespace SistemaDonacion.Models
{
    public class ApplicationUser
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(256)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        public string Contrasenia { get; set; } = string.Empty;

        public bool Estado { get; set; } = true;

        [MaxLength(50)]
        public string Rol { get; set; } = "Medico";


        public int? HospitalId { get; set; }
        public Hospital? Hospital { get; set; }
    }
}
