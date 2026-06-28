// AlexaController - Sistema de automatización de PC por voz mediante Alexa.
// Copyright (C) 2026  José Luis Bautista Martín
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

using AlexaController.Gestores;
using AlexaController.Helpers;
using AlexaController.Seguridad;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using Serilog;

const string MutexName = "Global\\AlexaController_SingleInstance";
using var mutex = new Mutex(true, MutexName, out bool esNuevaInstancia);
if (!esNuevaInstancia)
{
    Console.WriteLine("Ya hay una instancia de AlexaController en ejecución. Saliendo.");
    return;
}

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.WithMachineName()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.Configure<BasicAuthOptions>(builder.Configuration.GetSection("BasicAuth"));

builder.Services.AddAuthentication("BasicAuth")
    .AddScheme<AuthenticationSchemeOptions, BasicAuthHandler>("BasicAuth", null);

builder.Services.AddAuthorization();
builder.Services.AddControllers();

builder.Services.AddSingleton<StateManager>();
builder.Services.AddSingleton<ProgramManager>();
builder.Services.AddSingleton<ServiceManager>();
builder.Services.AddSingleton<EquipoHelper>();
builder.Services.AddSingleton<ProcesosHelper>();
builder.Services.AddSingleton<SteamHelper>();
builder.Services.AddSingleton<VolumeHelper>();
builder.Services.AddSingleton<JoypadHelper>();
builder.Services.AddSingleton<MonitorHelper>();
builder.Services.AddSingleton<JuegosHelper>();

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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AlexaController API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
{
    appBuilder.UseAuthentication();
});

app.UseAuthorization();
app.MapControllers();

Log.Information("AlexaController iniciado en {Url} (máquina: {Maquina})",
    builder.Configuration["Kestrel:Endpoints:Http:Url"] ?? "http://localhost:5780",
    Environment.MachineName);

app.Run();