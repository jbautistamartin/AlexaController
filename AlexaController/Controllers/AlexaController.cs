using AlexaController.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlexaController.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize] // Esto asegura que el endpoint está protegido por Basic Auth
    public class AlexaController : ControllerBase
    {
        private readonly ILogger<AlexaController> _logger;
        private readonly EquipoHelper _equipoHelper;
        private readonly JuegosHelper _juegosHelper;
        private readonly ProcesosHelper _procesosHelper;
        private readonly SteamHelper _steamHelper;

        public AlexaController(ILogger<AlexaController> logger, EquipoHelper equipoHelper, JuegosHelper juegosHelper, ProcesosHelper procesosHelper, SteamHelper steamHelper)
        {
            _logger = logger;
            this._equipoHelper = equipoHelper;
            this._juegosHelper = juegosHelper;
            this._procesosHelper = procesosHelper;
            this._steamHelper = steamHelper;
        }

        [HttpGet(nameof(ApagarEquipo))]
        public async Task ApagarEquipo()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Apagando equipo...");
                    _equipoHelper.ApagarEquipo();
                    _logger.LogInformation("Apagado de equipo finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al apagar el equipo.");
                }
            });
        }

        [HttpGet(nameof(ReiniciarEquipo))]
        public async Task ReiniciarEquipo()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando equipo...");
                    _equipoHelper.ReiniciarEquipo();
                    _logger.LogInformation("Reinicio de equipo finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar el equipo.");
                }
            });
        }

        [HttpGet(nameof(IniciarSteam))]
        public async Task IniciarSteam()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Iniciando Steam...");
                    _steamHelper.IniciarSteam();
                    _logger.LogInformation("Inicio de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al iniciar Steam.");
                }
            });
        }

        [HttpGet(nameof(CerrarSteam))]
        public async Task CerrarSteam()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Cerrando Steam...");
                    _steamHelper.CerrarSteam();
                    _logger.LogInformation("Cierre de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar Steam.");
                }
            });
        }

        [HttpGet(nameof(ReiniciarSteam))]
        public async Task ReiniciarSteam()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando Steam...");
                    _steamHelper.ReiniciarSteam();
                    _logger.LogInformation("Reinicio de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar Steam.");
                }
            });
        }

        [HttpGet(nameof(CerrarRetroArch))]
        public async Task CerrarRetroArch()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Cerrando RetroArch...");
                    _procesosHelper.CerrarRetroArch();
                    _logger.LogInformation("Cierre de RetroArch finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar RetroArch.");
                }
            });
        }

        [HttpGet(nameof(IniciarModoJuegos))]
        public async Task IniciarModoJuegos()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Iniciando Modo Juegos...");
                    _juegosHelper.IniciarModoJuegos();
                    _logger.LogInformation("Inicio de Modo Juegos finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al Iniciar Modo Juego.");
                }
            });
        }

        [HttpGet(nameof(DetenerModoJuegos))]
        public async Task DetenerModoJuegos()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Deteniendo Modo Juegos...");
                    _juegosHelper.DetenerModoJuegos();
                    _logger.LogInformation("Detención de Modo Juegos finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al Detener Modo Juego.");
                }
            });
        }
    }
}