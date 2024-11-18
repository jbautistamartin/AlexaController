using AlexaController.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlexaController.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize] // Protege los endpoints con autenticación
    public class AlexaController : ControllerBase
    {
        private readonly ILogger<AlexaController> _logger;
        private readonly EquipoHelper _equipoHelper;
        private readonly JuegosHelper _juegosHelper;
        private readonly ProcesosHelper _procesosHelper;
        private readonly SteamHelper _steamHelper;

        public AlexaController(
            ILogger<AlexaController> logger,
            EquipoHelper equipoHelper,
            JuegosHelper juegosHelper,
            ProcesosHelper procesosHelper,
            SteamHelper steamHelper)
        {
            _logger = logger;
            _equipoHelper = equipoHelper;
            _juegosHelper = juegosHelper;
            _procesosHelper = procesosHelper;
            _steamHelper = steamHelper;
        }

        [HttpGet(nameof(ApagarEquipo))]
        public IActionResult ApagarEquipo()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Apagando equipo...");
                    await _equipoHelper.ApagarEquipoAsync();
                    _logger.LogInformation("Apagado de equipo finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al apagar el equipo.");
                }
            });

            return Ok("El comando para apagar el equipo se ha enviado y se está ejecutando en segundo plano.");
        }

        [HttpGet(nameof(ReiniciarEquipo))]
        public IActionResult ReiniciarEquipo()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando equipo...");
                    await _equipoHelper.ReiniciarEquipoAsync();
                    _logger.LogInformation("Reinicio de equipo finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar el equipo.");
                }
            });

            return Ok("El comando para reiniciar el equipo se ha enviado y se está ejecutando en segundo plano.");
        }

        [HttpGet(nameof(IniciarSteam))]
        public IActionResult IniciarSteam()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Iniciando Steam...");
                    await _steamHelper.IniciarSteamAsync();
                    _logger.LogInformation("Inicio de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al iniciar Steam.");
                }
            });

            return Ok("El comando para iniciar Steam se ha enviado y se está ejecutando en segundo plano.");
        }

        [HttpGet(nameof(CerrarSteam))]
        public IActionResult CerrarSteam()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Cerrando Steam...");
                    await _steamHelper.CerrarSteamAsync();
                    _logger.LogInformation("Cierre de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar Steam.");
                }
            });

            return Ok("El comando para cerrar Steam se ha enviado y se está ejecutando en segundo plano.");
        }

        [HttpGet(nameof(ReiniciarSteam))]
        public IActionResult ReiniciarSteam()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando Steam...");
                    await _steamHelper.ReiniciarSteamAsync();
                    _logger.LogInformation("Reinicio de Steam finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar Steam.");
                }
            });

            return Ok("El comando para reiniciar Steam se ha enviado y se está ejecutando en segundo plano.");
        }

        [HttpGet(nameof(CerrarRetroArch))]
        public IActionResult CerrarRetroArch()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Cerrando RetroArch...");
                    await _procesosHelper.CerrarRetroArchAsync();
                    _logger.LogInformation("Cierre de RetroArch finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar RetroArch.");
                }
            });

            return Ok("El comando para cerrar RetroArch se ha enviado y se está ejecutando en segundo plano.");
        }

        [HttpGet(nameof(IniciarModoJuegos))]
        public IActionResult IniciarModoJuegos()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Iniciando Modo Juegos...");
                    await _juegosHelper.IniciarModoJuegosAsync();
                    _logger.LogInformation("Inicio de Modo Juegos finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al iniciar Modo Juegos.");
                }
            });

            return Ok("El comando para iniciar el Modo Juegos se ha enviado y se está ejecutando en segundo plano.");
        }

        [HttpGet(nameof(DetenerModoJuegos))]
        public IActionResult DetenerModoJuegos()
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Deteniendo Modo Juegos...");
                    await _juegosHelper.DetenerModoJuegosAsync();
                    _logger.LogInformation("Detención de Modo Juegos finalizado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al detener el Modo Juegos.");
                }
            });

            return Ok("El comando para detener el Modo Juegos se ha enviado y se está ejecutando en segundo plano.");
        }
    }
}