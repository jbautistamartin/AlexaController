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
        // Pasos que se anuncian en la ventana de progreso durante el arranque.
        private const int TotalPasosInicio = 7;

        private readonly ILogger<JuegosHelper> _logger;
        private readonly SteamHelper _steamHelper;
        private readonly ProgramManager _programManager;
        private readonly ServiceManager _serviceManager;
        private readonly MonitorHelper _monitorHelper;
        private readonly JoypadHelper _joypadHelper;
        private readonly VentanasHelper _ventanasHelper;
        private readonly ProgresoHelper _progresoHelper;

        private Dictionary<string, string> _procesosDetenidos = new();
        private List<string> _serviciosDesactivados = new();

        public JuegosHelper(
            ILogger<JuegosHelper> logger,
            SteamHelper steamHelper,
            ProgramManager programManager,
            ServiceManager serviceManager,
            MonitorHelper monitorHelper,
            JoypadHelper joypadHelper,
            VentanasHelper ventanasHelper,
            ProgresoHelper progresoHelper)
        {
            _logger = logger;
            _steamHelper = steamHelper;
            _programManager = programManager;
            _serviceManager = serviceManager;
            _monitorHelper = monitorHelper;
            _joypadHelper = joypadHelper;
            _ventanasHelper = ventanasHelper;
            _progresoHelper = progresoHelper;
        }

        internal async Task IniciarModoJuegosAsync()
        {
            var sw = Stopwatch.StartNew();

            // El arranque completo ronda el minuto: sin esta ventana no hay forma de saber
            // si está haciendo algo o se ha quedado colgado.
            _progresoHelper.Iniciar("Modo juegos", TotalPasosInicio);
            try
            {
                // Primero los programas de la lista: al matarlos se guarda su ruta para poder
                // restaurarlos al salir. Después se cierra lo que quede abierto.
                _progresoHelper.Paso("Cerrando los programas en segundo plano...");
                _procesosDetenidos = await _programManager.StopProgramsAsync();

                _progresoHelper.Paso("Cerrando las ventanas abiertas...");
                await _ventanasHelper.CerrarTodasLasVentanasAsync();

                _progresoHelper.Paso("Ajustando la pantalla...");
                _monitorHelper.ActivarSoloMonitorPrincipal();

                _progresoHelper.Paso("Reconectando el mando...");
                await _joypadHelper.ReconectarMandoAsync();

                _progresoHelper.Paso("Iniciando Steam...");
                await _steamHelper.IniciarSteamAsync();

                _progresoHelper.Paso("Desactivando los servicios innecesarios...");
                _serviciosDesactivados = await _serviceManager.DisableServicesAsync();

                _progresoHelper.Paso("Esperando a que Steam esté listo...");
                var steamListo = await _steamHelper.EsperarVentanaAsync();

                _logger.LogInformation("Modo juegos iniciado completamente en {Elapsed:0.0}s.", sw.Elapsed.TotalSeconds);

                await _progresoHelper.CompletarAsync(steamListo
                    ? "Todo listo. ¡A jugar!"
                    : "Listo, pero Steam está tardando más de lo normal.");
            }
            finally
            {
                // Pase lo que pase, la ventana no puede quedarse encima de Steam.
                _progresoHelper.Cerrar();
            }
        }

        internal async Task DetenerModoJuegosAsync()
        {
            var sw = Stopwatch.StartNew();

            // Por si se detiene el modo juegos mientras todavía se estaba iniciando.
            _progresoHelper.Cerrar();

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
