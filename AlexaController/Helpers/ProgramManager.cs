namespace AlexaController.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;

    class ProgramManager
    {
        // Lista de procesos a detener con comentarios sobre cada uno
        private static readonly List<string> Processes = new()
    {
        "Adobe",       // Procesos relacionados con Adobe (ejemplo: Acrobat, Photoshop, etc.)
        "GoogleDrive", // Sincronización de Google Drive
        "Greenshot",   // Capturador de pantalla
        "OneDrive",    // Sincronización de OneDrive
        "Teams",       // Microsoft Teams, consumo alto de recursos
        "Zoom",        // Aplicación de videoconferencias
        "Dropbox",     // Sincronización de Dropbox
        "Slack"        // Comunicación de equipos
    };

        // Almacena los procesos detenidos previamente
        private static readonly HashSet<string> StoppedProcesses = new();

        // Método para detener programas innecesarios
        public static void StopPrograms()
        {
            Console.WriteLine("Deteniendo programas innecesarios...");
            foreach (var processName in Processes)
            {
                try
                {
                    var processes = Process.GetProcessesByName(processName);
                    if (processes.Length == 0)
                    {
                        Console.WriteLine($"No se encontraron procesos activos de '{processName}'.");
                        continue;
                    }

                    foreach (var process in processes)
                    {
                        process.Kill();
                        process.WaitForExit();
                        StoppedProcesses.Add(processName); // Registrar como detenido
                        Console.WriteLine($"Proceso '{processName}' detenido.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al detener el proceso '{processName}': {ex.Message}");
                }
            }
        }

        // Método para reiniciar programas detenidos previamente (si es posible)
        public static void RestartPrograms()
        {
            Console.WriteLine("Intentando reiniciar los programas previamente detenidos...");
            foreach (var processName in StoppedProcesses)
            {
                try
                {
                    Process.Start(processName); // Intentar reiniciar el programa
                    Console.WriteLine($"Proceso '{processName}' reiniciado.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al reiniciar el proceso '{processName}': {ex.Message}");
                }
            }
            StoppedProcesses.Clear(); // Limpiar la lista de procesos detenidos
        }
    }

 

}
