using System.Diagnostics;

namespace AlexaController.Helpers
{
    public class EquipoHelper
    {
        private readonly ILogger<EquipoHelper> _logger;

        public EquipoHelper(ILogger<EquipoHelper> logger)
        {
            _logger = logger;
        }

        public async Task ApagarEquipoAsync()
        {
            await Task.Run(() =>
            {
                Process.Start("shutdown", "/s /t 0");
            });
        }

        public async Task ReiniciarEquipoAsync()
        {
            await Task.Run(() =>
            {
                Process.Start("shutdown", "/r /t 0");
            });
        }
    }
}