using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace SistemaDonacion.Controllers
{
    // Usamos ControllerBase porque es una API (no usamos vistas Razor/Vistas de C#)
    public class BaseController : ControllerBase
    {
        // 1. Obtener el ID del usuario logueado (útil para la bitácora)
        protected int GetUserId()
        {
            var claim = User.FindFirst("UsuarioId")?.Value;
            return string.IsNullOrEmpty(claim) ? 0 : int.Parse(claim);
        }

        // 2. Obtener el HospitalId (para filtrar por sede)
        protected int? GetUserHospitalId()
        {
            var claim = User.FindFirst("HospitalId")?.Value;
            // Si es "0" o nulo, significa que es Admin Global
            if (string.IsNullOrEmpty(claim) || claim == "0") return null;
            return int.Parse(claim);
        }

        // 3. Saber si es Administrador
        protected bool IsAdmin()
        {
            return User.IsInRole("Administrador") || User.IsInRole("Admin");
        }
    }
}