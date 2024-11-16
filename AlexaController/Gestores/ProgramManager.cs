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

        // Lista de procesos a detener con comentarios sobre cada uno
        private readonly List<string> Processes = new()
        {
            "Adobe",       // Procesos relacionados con Adobe (ejemplo: Acrobat, Photoshop, etc.)
            "GoogleDrive", // Sincronización de Google Drive
            "Greenshot",   // Capturador de pantalla
            "OneDrive",    // Sincronización de OneDrive
            "Teams",       // Microsoft Teams, consumo alto de recursos
            "Zoom",        // Aplicación de videoconferencias
            "Dropbox",     // Sincronización de Dropbox
            "Slack",        // Comunicación de equipos
            "PasswordSafe"
        };

        // Almacena los procesos detenidos previamente
        private readonly HashSet<string> StoppedProcesses = new();

        // Método para detener programas innecesarios
        public void StopPrograms()
        {
            _logger.LogInformation("Deteniendo programas innecesarios...");
            foreach (var processName in Processes)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);
                    if (processes.Length == 0)
                    {
                        _logger.LogInformation($"No se encontraron procesos activos de '{processName}'.");
                        continue;
                    }

                    foreach (var process in processes)
                    {
                        process.Kill();
                        process.WaitForExit();
                        StoppedProcesses.Add(processName); // Registrar como detenido
                        _logger.LogInformation($"Proceso '{processName}' detenido.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al detener el proceso '{processName}': {ex.Message}");
                }
            }
        }

        // Método para reiniciar programas detenidos previamente (si es posible)
        public void RestartPrograms()
        {
            _logger.LogInformation("Intentando reiniciar los programas previamente detenidos...");
            foreach (var processName in StoppedProcesses)
            {
                try
                {
                    Process.Start(processName); // Intentar reiniciar el programa
                    _logger.LogInformation($"Proceso '{processName}' reiniciado.");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error al reiniciar el proceso '{processName}': {ex.Message}");
                }
            }
        }
    }
}