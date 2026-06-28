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

using System.Collections.Concurrent;
using System.Diagnostics;

namespace AlexaController.Gestores
{
    public class ProgramManager
    {
        private readonly ILogger<ProgramManager> _logger;
        private readonly List<string> _procesos;

        public ProgramManager(ILogger<ProgramManager> logger, IConfiguration config)
        {
            _logger = logger;
            _procesos = config.GetSection("Procesos").Get<List<string>>() ?? new();
        }

        public async Task<Dictionary<string, string>> StopProgramsAsync()
        {
            _logger.LogInformation("Deteniendo programas innecesarios...");
            var detenidos = new ConcurrentDictionary<string, string>();
            var pidActual = Process.GetCurrentProcess().Id;

            await Task.WhenAll(_procesos.Select(async nombre =>
            {
                try
                {
                    var procesos = Process.GetProcessesByName(nombre);
                    if (procesos.Length == 0) return;

                    foreach (var proceso in procesos)
                    {
                        if (proceso.Id == pidActual) continue;

                        try
                        {
                            var ruta = proceso.MainModule?.FileName;
                            if (!string.IsNullOrEmpty(ruta))
                                detenidos[nombre] = ruta;

                            proceso.Kill();
                            await proceso.WaitForExitAsync();
                            _logger.LogInformation("Proceso '{Nombre}' detenido.", nombre);
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al buscar procesos para '{Nombre}'.", nombre);
                }
            }));

            return detenidos.ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        public async Task RestartProgramsAsync(Dictionary<string, string> procesosDetenidos)
        {
            if (procesosDetenidos.Count == 0)
            {
                _logger.LogWarning("No hay procesos registrados para reiniciar.");
                return;
            }

            _logger.LogInformation("Reiniciando {Count} programas previamente detenidos...", procesosDetenidos.Count);

            await Task.WhenAll(procesosDetenidos.Select(async kv =>
            {
                try
                {
                    Process.Start(kv.Value);
                    _logger.LogInformation("Proceso '{Nombre}' reiniciado desde '{Ruta}'.", kv.Key, kv.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar el proceso '{Nombre}'.", kv.Key);
                }
            }));
        }
    }
}