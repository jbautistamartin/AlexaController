using System.Diagnostics;
using System.IO;

namespace AlexaController.Helpers
{
    public class SteamHelper
    {
        private readonly ILogger<SteamHelper> _logger;
        private readonly ProcesosHelper _procesosHelper;


        public SteamHelper(ILogger<SteamHelper> logger, ProcesosHelper processHelper)
        {
            _logger = logger;
            _procesosHelper = processHelper;
   
        }

        public async Task IniciarSteamAsync()
        {
           RunSteam();

        }

        private static void RunSteam()
        {
            // Obtener el directorio del ejecutable actual
            string currentDirectory = @"C:\Program Files (x86)\Steam\";

            // Configurar el proceso para ejecutarse en la misma carpeta y agregar el parámetro
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(currentDirectory, "steam.exe"),
                WorkingDirectory = currentDirectory,
                Arguments = "-cef-disable-sandbox"  // Agregar el parámetro
            };

            Process.Start(startInfo);
        }

        public async Task CerrarSteamAsync()
        {
            StopSteam();
 
        }

        private void StopSteam()
        {
            foreach (var process in Process.GetProcessesByName("steam"))
            {
                _procesosHelper.KillProcessAndChildrenAsync(process.Id).Wait();
            }
        }

        public async Task ReiniciarSteamAsync()
        {
            await CerrarSteamAsync();
            await Task.Delay(1000); // Espera un segundo de forma asincrónica
            await IniciarSteamAsync();
        }
    }
}