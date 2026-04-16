using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace SistemaDonacion.Filters
{
    /// <summary>
    /// Atributo personalizado para validar roles en endpoints protegidos.
    /// Solo permite acceso si el usuario tiene al menos uno de los roles especificados.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class AuthorizeRoleAttribute : Attribute, IAsyncAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRoleAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Validar autenticación
            if (!context.HttpContext.User.Identity?.IsAuthenticated ?? false)
            {
                context.Result = new UnauthorizedObjectResult(new
                {
                    message = "No autenticado",
                    code = "UNAUTHORIZED"
                });
                return;
            }

            // Obtener rol del usuario
            var userRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var userName = context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value ?? "desconocido";

            // Validar rol
            if (!_roles.Any(r => r.Equals(userRole, StringComparison.OrdinalIgnoreCase)))
            {
                // Log del intento de acceso denegado
                var logger = context.HttpContext.RequestServices.GetService<ILogger<AuthorizeRoleAttribute>>();
                logger?.LogWarning("[ACCESO DENEGADO] Usuario: {User} | Rol: {Role} | Ruta: {Path} | Timestamp: {Timestamp}",
                    userName, userRole, context.HttpContext.Request.Path, DateTime.UtcNow);

                context.Result = new ForbidResult();
                return;
            }
        }
    }
}

