using AlexaController.Gestores;

namespace AlexaController.Helpers
{
    public class JuegosHelper
    {
        private readonly ILogger<JuegosHelper> _logger;
        private readonly MonitorManager _monitorManager;
        private readonly ProgramManager _programManager;
        private readonly ServiceManager _serviceManager;

        public JuegosHelper(ILogger<JuegosHelper> logger, MonitorManager monitorManager, ProgramManager programManager, ServiceManager serviceManager)
        {
            _logger = logger;
            _monitorManager = monitorManager;
            _programManager = programManager;
            _serviceManager = serviceManager;
        }

        internal void DetenerModoJuegos()
        {
            _serviceManager.EnableServices();
            _programManager.RestartPrograms();
            _monitorManager.ConfigureDualMonitors();
        }

        internal void IniciarModoJuegos()
        {
            _monitorManager.ConfigureSingleMonitor();
            _programManager.StopPrograms();
            _serviceManager.DisableServices();
        }
    }
}