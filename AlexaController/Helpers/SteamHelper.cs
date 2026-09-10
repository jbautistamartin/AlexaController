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
    public class SteamHelper
    {
        private readonly ILogger<SteamHelper> _logger;
        private readonly ProcesosHelper _procesosHelper;
        private readonly VentanasHelper _ventanasHelper;
        private readonly string _steamPath;
        private readonly string _argumentos;
        private readonly bool _bigPicture;

        // Se completa cuando aparece la ventana principal de Steam tras un arranque.
        private TaskCompletionSource<bool>? _ventanaLista;

        public SteamHelper(
            ILogger<SteamHelper> logger,
            ProcesosHelper processHelper,
            VentanasHelper ventanasHelper,
            IConfiguration config)
        {
            _logger = logger;
            _procesosHelper = processHelper;
            _ventanasHelper = ventanasHelper;
            _steamPath = config["SteamPath"] ?? @"C:\Program Files (x86)\Steam\";
            _argumentos = config["SteamArgumentos"] ?? "-cef-disable-sandbox";
            _bigPicture = config.GetValue("SteamBigPicture", true);
        }

        private string RutaSteamExe => Path.Combine(_steamPath, "steam.exe");

        public async Task IniciarSteamAsync()
        {
            if (!Process.GetProcessesByName("JoyToKey").Any())
            {
                Process.Start(@"C:\Program Files (x86)\JoyToKey\JoyToKey.exe");
                _logger.LogInformation("JoyToKey iniciado.");
            }

            // -gamepadui arranca directamente en Big Picture (interfaz de mando).
            var argumentos = _bigPicture ? $"{_argumentos} -gamepadui".Trim() : _argumentos;

            if (Process.GetProcessesByName("steam").Any())
            {
                // Steam ya está en marcha: los argumentos de arranque se ignoran,
                // hay que pedirle el cambio a Big Picture por su protocolo.
                _logger.LogInformation("Steam ya estaba en ejecución.");
                if (_bigPicture)
                    AbrirBigPicture();
            }
            else
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = RutaSteamExe,
                    WorkingDirectory = _steamPath,
                    Arguments = argumentos
                };
                Process.Start(startInfo);
                _logger.LogInformation("Steam iniciado desde '{Path}' con argumentos '{Args}'.", _steamPath, argumentos);
            }

            // Sin await: no bloquea el inicio del modo juegos
            _ventanaLista = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _ = Task.Run(ColocarSteamAlFrenteAsync);
        }

        /// <summary>
        /// Espera a que la ventana principal de Steam aparezca tras el último arranque.
        /// Devuelve false si se agota la espera o si Steam no se ha iniciado desde aquí.
        /// </summary>
        public Task<bool> EsperarVentanaAsync() => _ventanaLista?.Task ?? Task.FromResult(false);

        /// <summary>
        /// Fuerza el modo Big Picture en una instancia de Steam que ya está arrancada.
        /// </summary>
        public void AbrirBigPicture()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = RutaSteamExe,
                    WorkingDirectory = _steamPath,
                    Arguments = "steam://open/bigpicture"
                };
                Process.Start(startInfo);
                _logger.LogInformation("Solicitado el modo Big Picture a Steam.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir el modo Big Picture.");
            }
        }

        private async Task ColocarSteamAlFrenteAsync()
        {
            // Espera hasta 60 segundos a que aparezca la ventana principal de Steam
            for (int i = 0; i < 60; i++)
            {
                await Task.Delay(1000);

                var hWnd = BuscarVentanaPrincipalSteam();
                if (hWnd == IntPtr.Zero)
                    continue;

                // Steam ya es utilizable: quien estuviera esperando (la ventana de progreso)
                // debe enterarse antes de que se le dé el foco.
                _ventanaLista?.TrySetResult(true);

                // Mantiene TOPMOST 8 segundos para superar diálogos tardíos, luego lo quita
                await _ventanasHelper.TraerAlFrenteAsync(hWnd, "Steam", msTopmost: 8000);
                return;
            }

            _ventanaLista?.TrySetResult(false);
            _logger.LogWarning("No se encontró la ventana principal de Steam en 60 segundos.");
        }

        /// <summary>
        /// Trae al frente la ventana principal de Steam si está en ejecución.
        /// </summary>
        public async Task EnfocarSteamAsync()
        {
            var hWnd = BuscarVentanaPrincipalSteam();
            if (hWnd == IntPtr.Zero)
            {
                _logger.LogWarning("No se encontró la ventana principal de Steam.");
                return;
            }

            await _ventanasHelper.TraerAlFrenteAsync(hWnd, "Steam");
        }

        /// <summary>
        /// En Big Picture (-gamepadui) la ventana pertenece a steamwebhelper, no a steam.exe,
        /// así que hay que buscar en ambos procesos.
        /// </summary>
        private static IntPtr BuscarVentanaPrincipalSteam()
        {
            foreach (var nombre in new[] { "steamwebhelper", "steam" })
            {
                foreach (var proc in Process.GetProcessesByName(nombre))
                {
                    if (proc.MainWindowHandle == IntPtr.Zero) continue;

                    var titulo = proc.MainWindowTitle;
                    if (titulo.Contains("Steam", StringComparison.OrdinalIgnoreCase) ||
                        titulo.Contains("Big Picture", StringComparison.OrdinalIgnoreCase))
                        return proc.MainWindowHandle;
                }
            }
            return IntPtr.Zero;
        }

        public async Task CerrarSteamAsync()
        {
            foreach (var proceso in Process.GetProcessesByName("JoyToKey"))
                await _procesosHelper.KillProcessAndChildrenAsync(proceso.Id);
            _logger.LogInformation("JoyToKey cerrado.");

            foreach (var proceso in Process.GetProcessesByName("steam"))
                await _procesosHelper.KillProcessAndChildrenAsync(proceso.Id);

            // En Big Picture steamwebhelper puede sobrevivir a su proceso padre.
            foreach (var proceso in Process.GetProcessesByName("steamwebhelper"))
                await _procesosHelper.KillProcessAndChildrenAsync(proceso.Id);
        }

        public async Task ReiniciarSteamAsync()
        {
            await CerrarSteamAsync();
            await Task.Delay(1000);
            await IniciarSteamAsync();
        }
    }
}
