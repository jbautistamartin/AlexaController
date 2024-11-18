using System.Collections.Concurrent;
using System.Diagnostics;

namespace AlexaController.Gestores
{
    public class ProgramManager
    {
        private readonly ILogger<ProgramManager> _logger;

        public ProgramManager(ILogger<ProgramManager> logger)
        {
            _logger = logger;
        }

        // Lista de procesos a detener
        private readonly List<string> Processes = new()
        {
            "GoogleDriveFS",
            "Greenshot",
            "OfficeClickToRun",
            "PerfWatson2",
            "PhoneExperienceHost",
            "SearchApp",
            "SearchProtocolHost",
            "audiodg",
            "dwm",
            "ms-teams",
            "msedgewebview2",
            "pwsafe",
            "smartscreen",
            "smss",
            "Adobe",       // Procesos relacionados con Adobe (ejemplo: Acrobat, Photoshop, etc.)
            "GoogleDrive", // Sincronización de Google Drive
            "Greenshot",   // Capturador de pantalla
            "OneDrive",    // Sincronización de OneDrive
            "Teams",       // Microsoft Teams, consumo alto de recursos
            "Zoom",        // Aplicación de videoconferencias
            "Dropbox",     // Sincronización de Dropbox
            "Slack",        // Comunicación de equipos
#if !DEBUG
            "chrome"
#endif
        };

        // Almacena las rutas de procesos detenidos
        private readonly ConcurrentDictionary<string, string> StoppedProcesses = new();

        // Método para detener programas de manera concurrente
        public async Task StopProgramsAsync()
        {
            _logger.LogInformation("Deteniendo programas innecesarios...");

            await Task.WhenAll(Processes.Select(async processName =>
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);
                    if (processes.Length == 0)
                        return;

                    foreach (var process in processes)
                    {
                        try
                        {
                            // Registrar ruta del ejecutable antes de detener
                            string executablePath = process.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(executablePath))
                            {
                                StoppedProcesses[processName] = executablePath;
                            }

                            process.Kill();
                            await process.WaitForExitAsync(); // Asíncrono en .NET 5 o superior
                            _logger.LogInformation($"Proceso '{processName}' detenido.");
                        }
                        catch
                        {
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al buscar procesos para '{processName}': {ex.Message}");
                }
            }));
        }

        // Método para reiniciar programas detenidos de manera concurrente
        public async Task RestartProgramsAsync()
        {
            _logger.LogInformation("Intentando reiniciar los programas previamente detenidos...");

            await Task.WhenAll(StoppedProcesses.Select(async kvp =>
            {
                var (processName, executablePath) = kvp;
                try
                {
                    if (!string.IsNullOrEmpty(executablePath))
                    {
                        Process.Start(executablePath);
                        _logger.LogInformation($"Proceso '{processName}' reiniciado desde '{executablePath}'.");
                    }
                    else
                    {
                        _logger.LogWarning($"No se pudo reiniciar '{processName}' porque no se encontró la ruta del ejecutable.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al reiniciar el proceso '{processName}': {ex.Message}");
                }
            }));
        }
    }
}