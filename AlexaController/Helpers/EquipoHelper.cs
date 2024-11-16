using Microsoft.AspNetCore.Mvc;
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

        public void ApagarEquipo()
        {
            Process.Start("shutdown", "/s /t 0");
        }

        [HttpGet]
        public void ReiniciarEquipo()
        {
            Process.Start("shutdown", "/r /t 0");
        }
    }
}