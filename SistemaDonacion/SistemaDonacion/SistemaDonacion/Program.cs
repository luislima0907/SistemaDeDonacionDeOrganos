using Microsoft.EntityFrameworkCore;
using SistemaDonacion.Data;
using SistemaDonacion.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using SistemaDonacion.Components;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add Antiforgery services
builder.Services.AddAntiforgery();

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
    });

// Register password hash service
builder.Services.AddScoped<IPasswordHashService, PasswordHashService>();

builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Development: allow HTTP
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Serve static files
app.UseStaticFiles();

// Explicit routing
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Add Antiforgery middleware
app.UseAntiforgery();

app.MapControllers();

// Map root to login.html
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