using AlexaController.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace AlexaController.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlexaController : ControllerBase
    {
        private readonly ILogger<AlexaController> _logger;

        public AlexaController(ILogger<AlexaController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public async Task ApagarEquipo()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Apagando equipo...");
                    EquipoHelper.ApagarEquipo();
                    _logger.LogInformation("Apagado de equipo finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al apagar el equipo.");
                }
            });
        }

        [HttpGet]
        public async Task ReiniciarEquipo()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando equipo...");
                    EquipoHelper.ReiniciarEquipo();
                    _logger.LogInformation("Reinicio de equipo finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar el equipo.");
                }
            });
        }

        [HttpGet]
        public async Task IniciarSteam()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Iniciando Steam...");
                    SteamHelper.IniciarSteam();
                    _logger.LogInformation("Inicio de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al iniciar Steam.");
                }
            });
        }

        [HttpGet]
        public async Task CerrarSteam()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Cerrando Steam...");
                    SteamHelper.CerrarSteam();
                    _logger.LogInformation("Cierre de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar Steam.");
                }
            });
        }

        [HttpGet]
        public async Task ReiniciarSteam()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando Steam...");
                    SteamHelper.ReiniciarSteam();
                    _logger.LogInformation("Reinicio de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar Steam.");
                }
            });
        }

        [HttpGet]
        public async Task CerrarRetroArch()
        {
            await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Cerrando RetroArch...");
                    ProcesosHelper.CerrarRetroArch();
                    _logger.LogInformation("Cierre de RetroArch finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar RetroArch.");
                }
            });
        }

        [HttpGet]
        public async Task IniciarModoJuegos()
        {
            await Task.Run(() =>
            {
            try
            {
                _logger.LogInformation("Iniciando Modo Juegos...");
                JuegosHelper.IniciarModoJuegos();
                _logger.LogInformation("Inicio de Modo Juegos finalizado.");
            }
            catch (Exception ex)


