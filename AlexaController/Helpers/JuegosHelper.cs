using AlexaController.Gestores;

namespace AlexaController.Helpers
{
    public class JuegosHelper
    {
        private readonly ILogger<JuegosHelper> _logger;
        private readonly SteamHelper _steamHelper;
        private readonly ProgramManager _programManager;
        private readonly ServiceManager _serviceManager;

        public JuegosHelper(ILogger<JuegosHelper> logger, SteamHelper steamHelper, ProgramManager programManager, ServiceManager serviceManager)
        {
            _logger = logger;
            _steamHelper = steamHelper;
            _programManager = programManager;
            _serviceManager = serviceManager;
        }

        internal async Task DetenerModoJuegosAsync()
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(_steamHelper.CerrarSteamAsync());
            tasks.Add(_serviceManager.EnableServicesAsync());
            tasks.Add(_programManager.RestartProgramsAsync());
            await Task.WhenAll(tasks); // Ejecutar todas las tareas de forma concurrente
        }

        internal async Task IniciarModoJuegosAsync()
        {
            List<Task> tasks = new List<Task>();

            tasks.Add(_programManager.StopProgramsAsync());
            tasks.Add(_serviceManager.DisableServicesAsync());
            tasks.Add(_steamHelper.IniciarSteamAsync());

            await Task.WhenAll(tasks); // Ejecutar todas las tareas de forma concurrente
        }
    }
}