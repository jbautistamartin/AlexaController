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

using System.Runtime.Versioning;
using System.ServiceProcess;

namespace AlexaController.Gestores
{
    [SupportedOSPlatform("windows")]
    public class ServiceManager
    {
        private readonly ILogger<ServiceManager> _logger;
        private readonly List<string> _servicios;

        public ServiceManager(ILogger<ServiceManager> logger, IConfiguration config)
        {
            _logger = logger;
            _servicios = config.GetSection("Servicios").Get<List<string>>() ?? new();
        }

        public async Task<List<string>> DisableServicesAsync()
        {
            _logger.LogInformation("Desactivando servicios innecesarios...");
            var desactivados = new System.Collections.Concurrent.ConcurrentBag<string>();

            await Task.WhenAll(_servicios.Select(async nombre =>
            {
                try
                {
                    using var servicio = new ServiceController(nombre);
                    if (servicio.Status == ServiceControllerStatus.Running)
                    {
                        servicio.Stop();
                        desactivados.Add(nombre);
                        _logger.LogInformation("Servicio '{Nombre}' desactivado.", nombre);
                    }
                }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al desactivar el servicio '{Nombre}'.", nombre);
                }
            }));

            return desactivados.ToList();
        }

        public async Task EnableServicesAsync(List<string> serviciosDesactivados)
        {
            if (serviciosDesactivados.Count == 0)
            {
                _logger.LogWarning("No hay servicios registrados para reactivar.");
                return;
            }

            _logger.LogInformation("Reactivando {Count} servicios previamente desactivados...", serviciosDesactivados.Count);

            await Task.WhenAll(serviciosDesactivados.Select(async nombre =>
            {
                try
                {
                    using var servicio = new ServiceController(nombre);
                    if (servicio.Status == ServiceControllerStatus.Stopped)
                    {
                        servicio.Start();
                        _logger.LogInformation("Servicio '{Nombre}' reactivado.", nombre);
                    }
                }
                catch (InvalidOperationException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reactivar el servicio '{Nombre}'.", nombre);
                }
            }));
        }
    }
}