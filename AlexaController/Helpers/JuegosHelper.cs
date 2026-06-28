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

using AlexaController.Gestores;
using System.Diagnostics;

namespace AlexaController.Helpers
{
    public class JuegosHelper
    {
        private readonly ILogger<JuegosHelper> _logger;
        private readonly SteamHelper _steamHelper;
        private readonly ProgramManager _programManager;
        private readonly ServiceManager _serviceManager;
        private readonly StateManager _stateManager;
        private readonly MonitorHelper _monitorHelper;
        private readonly JoypadHelper _joypadHelper;

        public JuegosHelper(
            ILogger<JuegosHelper> logger,
            SteamHelper steamHelper,
            ProgramManager programManager,
            ServiceManager serviceManager,
            StateManager stateManager,
            MonitorHelper monitorHelper,
            JoypadHelper joypadHelper)
        {
            _logger = logger;
            _steamHelper = steamHelper;
            _programManager = programManager;
            _serviceManager = serviceManager;
            _stateManager = stateManager;
            _monitorHelper = monitorHelper;
            _joypadHelper = joypadHelper;
        }

        internal async Task IniciarModoJuegosAsync()
        {
            var estado = _stateManager.ObtenerEstado();
            if (estado.Activo || estado.Iniciando)
            {
                _logger.LogWarning("Modo juegos ya está {Estado}. Se ignora la petición de inicio.", estado.Activo ? "activo" : "iniciando");
                return;
            }

            var sw = Stopwatch.StartNew();
            _stateManager.SetIniciando();

            var modoMonitorAnterior = _monitorHelper.ObtenerModoActual();
            _monitorHelper.ActivarSoloMonitorPrincipal();

            var tareaProcesos = _programManager.StopProgramsAsync();
            var procesos = await tareaProcesos;
            var tareaServicios = _serviceManager.DisableServicesAsync();
            var servicios = await tareaServicios;
            var tareaMando = _joypadHelper.ReconectarMandoAsync();
            await tareaMando;

            await _steamHelper.IniciarSteamAsync();

            _stateManager.SetActivo(procesos, servicios, modoMonitorAnterior);
            _logger.LogInformation("Modo juegos iniciado completamente en {Elapsed:0.0}s.", sw.Elapsed.TotalSeconds);
        }

        internal async Task DetenerModoJuegosAsync()
        {
            var estado = _stateManager.ObtenerEstado();
            if (!estado.Activo)
            {
                _logger.LogWarning("Modo juegos no está activo. Se ignora la petición de detención.");
                return;
            }

            var sw = Stopwatch.StartNew();

            await Task.WhenAll(
                _steamHelper.CerrarSteamAsync(),
                _serviceManager.EnableServicesAsync(estado.ServiciosDesactivados),
                _programManager.RestartProgramsAsync(estado.ProcesosDetenidos)
            );

            _monitorHelper.AplicarModo("extend");

            _stateManager.SetDetenido();
            _logger.LogInformation("Modo juegos detenido completamente en {Elapsed:0.0}s.", sw.Elapsed.TotalSeconds);
        }
    }
}