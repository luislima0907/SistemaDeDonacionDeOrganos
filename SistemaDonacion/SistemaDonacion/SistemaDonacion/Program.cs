using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaDonacion.Components;
using System.IO;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Antiforgery services
builder.Services.AddAntiforgery();

// HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Add DbContext (SQL Server) - tabla Usuarios
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Authentication with Cookie scheme
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.Cookie.Name = "SistemaDonacion.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/login.html";
        options.AccessDeniedPath = "/acceso-denegado.html";
    });

// Services
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IRankingService, RankingService>();

// Configurar JSON para ignorar referencias circulares
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Routing
app.UseRouting();

// Authentication y Authorization antes de los archivos estáticos
app.UseAuthentication();
app.UseAuthorization();

// Rutas protegidas
app.MapGet("/admin.html", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.Redirect("/login.html");
        return;
    }

    var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    if (!role.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/acceso-denegado.html");
        return;
    }

    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "admin.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(path);
});

app.MapGet("/medico.html", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.Redirect("/login.html");
        return;
    }

    var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    if (!role.Equals("Medico", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/acceso-denegado.html");
        return;
    }

    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "medico.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(path);
});

app.MapGet("/donantes.html", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.Redirect("/login.html");
        return;
    }

    var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    if (!role.Equals("Medico", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/acceso-denegado.html");
        return;
    }

    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "donantes.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(path);
});

app.MapGet("/pacientes.html", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.Redirect("/login.html");
        return;
    }

    var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    if (!role.Equals("Medico", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/acceso-denegado.html");
        return;
    }

    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "pacientes.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(path);
});

app.MapGet("/ranking.html", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.Redirect("/login.html");
        return;
    }

    var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    if (!role.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/acceso-denegado.html");
        return;
    }

    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "ranking.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(path);
});

app.MapGet("/bitacora.html", async context =>
{
    if (!context.User.Identity?.IsAuthenticated ?? true)
    {
        context.Response.Redirect("/login.html");
        return;
    }

    var role = context.User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";

    if (!role.Equals("Administrador", StringComparison.OrdinalIgnoreCase) &&
        !role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Redirect("/acceso-denegado.html");
        return;
    }

    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "bitacora.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(path);
});

app.MapGet("/acceso-denegado.html", async context =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "acceso-denegado.html");
    context.Response.ContentType = "text/html; charset=utf-8";
    await context.Response.SendFileAsync(path);
});

// Middleware
app.UseAntiforgery();

// Archivos estáticos después de las validaciones
app.UseStaticFiles();

app.MapControllers();

// Root
app.MapGet("/", async context =>
{
    var path = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "login.html");

    if (File.Exists(path))
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.SendFileAsync(path);
    }
    else
    {
        context.Response.StatusCode = 404;
        await context.Response.WriteAsync("login.html not found");
    }
});

app.Run();