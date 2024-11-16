using AlexaController.Gestores;  // Asegúrate de que los namespaces sean correctos
using AlexaController.Helpers;
using AlexaController.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/AlexaController.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Cargar la configuración desde appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// Configuración de BasicAuthOptions desde appsettings.json
builder.Services.Configure<BasicAuthOptions>(builder.Configuration.GetSection("BasicAuth"));

// Configuración de autenticación y autorización
builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Registrar las clases como Singleton
builder.Services.AddSingleton<MonitorManager>();
builder.Services.AddSingleton<ProgramManager>();
builder.Services.AddSingleton<ServiceManager>();
builder.Services.AddSingleton<EquipoHelper>();
builder.Services.AddSingleton<JuegosHelper>();
builder.Services.AddSingleton<ProcesosHelper>();
builder.Services.AddSingleton<SteamHelper>();

// Configurar Swagger
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AlexaController API",
            Version = "v1",
            Description = "API para manejar el control de Alexa"
        });
    });
}

var app = builder.Build();

// Habilitar Swagger y la UI en /swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AlexaController API v1");
        options.RoutePrefix = "swagger"; // Cambiar el prefijo de la ruta a "swagger"
    });
}

// Excluir rutas de Swagger del middleware de autenticación
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
{
    appBuilder.UseAuthentication(); // Solo se aplica autenticación fuera de Swagger
});

// Middleware de autorización
app.UseAuthorization();

app.MapControllers();

app.Run();