using SistemaDonacion.Data;
using SistemaDonacion.Models;

namespace SistemaDonacion.Services
{
    public interface IBitacoraService
    {
        Task RegistrarAccionAsync(int usuarioId, string accion, string tabla, int registroId, 
            string? datosAnteriores = null, string? datosNuevos = null, string? detalles = null);
    }

    public class BitacoraService : IBitacoraService
    {
        private readonly AppDbContext _context;

        public BitacoraService(AppDbContext context)
        {
            _context = context;
        }

        public async Task RegistrarAccionAsync(int usuarioId, string accion, string tabla, 
            int registroId, string? datosAnteriores = null, string? datosNuevos = null, 
            string? detalles = null)
        {
            var bitacora = new BitacoraAccion
            {
                UsuarioId = usuarioId,
                Accion = accion,
                Tabla = tabla,
                RegistroId = registroId,
                DatosAnteriores = datosAnteriores,
                DatosNuevos = datosNuevos,
                Detalles = detalles,
                FechaAccion = DateTime.Now
            };

            _context.BitacoraAcciones.Add(bitacora);
            await _context.SaveChangesAsync();
        }
    }
}

