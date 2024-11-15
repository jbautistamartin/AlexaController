using AlexaController.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog para que registre los logs en un archivo
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()  // Solo registra logs de nivel "Information" o superior
    .WriteTo.File("logs/AlexaController.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

// Configurar Serilog como el proveedor de logging para la aplicación
builder.Host.UseSerilog();

// Configuración de Basic Auth para la Web API
builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);

builder.Services.AddAuthorization();

// Add services to the container.
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseAuthentication(); // Usar autenticación
app.UseAuthorization(); // Usar autorización

app.MapControllers();

app.Run();