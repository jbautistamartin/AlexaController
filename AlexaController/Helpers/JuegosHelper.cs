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
        private readonly MonitorHelper _monitorHelper;
        private readonly JoypadHelper _joypadHelper;

        private Dictionary<string, string> _procesosDetenidos = new();
        private List<string> _serviciosDesactivados = new();

        public JuegosHelper(
            ILogger<JuegosHelper> logger,
            SteamHelper steamHelper,
            ProgramManager programManager,
            ServiceManager serviceManager,
            MonitorHelper monitorHelper,
            JoypadHelper joypadHelper)
        {
            _logger = logger;
            _steamHelper = steamHelper;
            _programManager = programManager;
            _serviceManager = serviceManager;
            _monitorHelper = monitorHelper;
            _joypadHelper = joypadHelper;
        }

        internal async Task IniciarModoJuegosAsync()
        {
            var sw = Stopwatch.StartNew();

            _monitorHelper.ActivarSoloMonitorPrincipal();

            await _joypadHelper.ReconectarMandoAsync();
            await _steamHelper.IniciarSteamAsync();

            _procesosDetenidos = await _programManager.StopProgramsAsync();
            _serviciosDesactivados = await _serviceManager.DisableServicesAsync();

            _logger.LogInformation("Modo juegos iniciado completamente en {Elapsed:0.0}s.", sw.Elapsed.TotalSeconds);
        }

        internal async Task DetenerModoJuegosAsync()
        {
            var sw = Stopwatch.StartNew();

            await Task.WhenAll(
                _steamHelper.CerrarSteamAsync(),
                _serviceManager.EnableServicesAsync(_serviciosDesactivados),
                _programManager.RestartProgramsAsync(_procesosDetenidos)
            );

            _monitorHelper.AplicarModo("extend");

            _procesosDetenidos = new();
            _serviciosDesactivados = new();
            _logger.LogInformation("Modo juegos detenido completamente en {Elapsed:0.0}s.", sw.Elapsed.TotalSeconds);
        }
    }
}
