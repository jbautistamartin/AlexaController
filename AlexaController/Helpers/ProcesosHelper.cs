using System.Diagnostics;

namespace AlexaController.Helpers
{
    public class ProcesosHelper
    {
        private readonly ILogger<ProcesosHelper> _logger;

        public ProcesosHelper(ILogger<ProcesosHelper> logger)
        {
            _logger = logger;
        }

        public async Task CerrarRetroArchAsync()
        {
            await Task.Run(async () =>
            {
                foreach (var process in Process.GetProcessesByName("retroarch"))
                {
                    await KillProcessAndChildrenAsync(process.Id);
                }
            });
        }

        public async Task KillProcessAndChildrenAsync(int pid)
        {
            try
            {
                var process = Process.GetProcessById(pid);
                if (process == null || process.HasExited) return;

                // Mata el proceso y espera su salida de manera asincrónica
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
            catch (Exception ex)
            {
                // Manejo de excepciones opcional, registra errores si es necesario
                Console.WriteLine($"Error al intentar matar el proceso {pid}: {ex.Message}");
            }
        }

    }
}