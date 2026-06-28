// AlexaController - Sistema de automatización de PC por voz mediante Alexa.
// Copyright (C) 2026  José Luis Bautista Martín
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

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
                var procesos = Process.GetProcessesByName("retroarch");
                if (procesos.Length == 0)
                {
                    _logger.LogInformation("RetroArch no estaba en ejecución.");
                    return;
                }

                foreach (var process in procesos)
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

                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                _logger.LogInformation("Proceso {PID} ({Nombre}) terminado.", pid, process.ProcessName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar matar el proceso {PID}.", pid);
            }
        }
    }
}