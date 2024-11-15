using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace AlexaController.Helpers
{
    static class EquipoHelper
    {
     
        public static void ApagarEquipo()
        {
            Process.Start("shutdown", "/s /t 0");
        }

        [HttpGet]
        public static void ReiniciarEquipo()
        {
            Process.Start("shutdown", "/r /t 0");
        }
    }
}
