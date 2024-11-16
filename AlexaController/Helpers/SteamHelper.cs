using System.Diagnostics;

namespace AlexaController.Helpers
{
    public class SteamHelper
    {
        private readonly ILogger<SteamHelper> _logger;
        private readonly ProcesosHelper _procesosHelper;
        private readonly JuegosHelper _juegosHelper;

        public SteamHelper(ILogger<SteamHelper> logger, ProcesosHelper processHelper, JuegosHelper juegosHelper)
        {
            _logger = logger;
            _procesosHelper = processHelper;
            _juegosHelper = juegosHelper;
        }

        public void IniciarSteam()
        {
            _juegosHelper.IniciarModoJuegos();
            Process.Start("steam://run");
        }

        public void CerrarSteam()
        {
            foreach (var process in Process.GetProcessesByName("steam"))
            {
                _procesosHelper.KillProcessAndChildren(process.Id);
            }
            _juegosHelper.DetenerModoJuegos();
        }

        public void ReiniciarSteam()
        {
            foreach (var process in Process.GetProcessesByName("steam"))
            {
                _procesosHelper.KillProcessAndChildren(process.Id);
            }
            Thread.Sleep(1000); // Espera un segundo antes de reiniciar
            Process.Start("steam://run");
        }
    }
}