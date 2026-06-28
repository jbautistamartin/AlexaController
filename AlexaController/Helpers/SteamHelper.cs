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
using System.Runtime.InteropServices;

namespace AlexaController.Helpers
{
    public class SteamHelper
    {
        private readonly ILogger<SteamHelper> _logger;
        private readonly ProcesosHelper _procesosHelper;
        private readonly string _steamPath;

        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        public SteamHelper(ILogger<SteamHelper> logger, ProcesosHelper processHelper, IConfiguration config)
        {
            _logger = logger;
            _procesosHelper = processHelper;
            _steamPath = config["SteamPath"] ?? @"C:\Program Files (x86)\Steam\";
        }

        public async Task IniciarSteamAsync()
        {
            if (!Process.GetProcessesByName("JoyToKey").Any())
            {
                Process.Start(@"C:\Program Files (x86)\JoyToKey\JoyToKey.exe");
                _logger.LogInformation("JoyToKey iniciado.");
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = Path.Combine(_steamPath, "steam.exe"),
                WorkingDirectory = _steamPath,
                Arguments = "-cef-disable-sandbox"
            };
            Process.Start(startInfo);
            _logger.LogInformation("Steam iniciado desde '{Path}'.", _steamPath);

            // Sin await: no bloquea el inicio del modo juegos
            _ = Task.Run(ColocarSteamAlFrenteAsync);
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

                ShowWindow(hWnd, SW_RESTORE);
                // TOPMOST garantiza que esté por encima de cualquier diálogo
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                SetForegroundWindow(hWnd);
                _logger.LogInformation("Steam colocado al frente (TOPMOST).");

                // Mantiene TOPMOST 8 segundos para superar diálogos tardíos, luego lo quita
                await Task.Delay(8000);
                SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                _logger.LogInformation("Steam: TOPMOST eliminado, comportamiento normal restaurado.");
                return;
            }

            _logger.LogWarning("No se encontró la ventana principal de Steam en 60 segundos.");
        }

        private static IntPtr BuscarVentanaPrincipalSteam()
        {
            foreach (var proc in Process.GetProcessesByName("steam"))
            {
                if (proc.MainWindowHandle != IntPtr.Zero &&
                    proc.MainWindowTitle.Contains("Steam", StringComparison.OrdinalIgnoreCase))
                    return proc.MainWindowHandle;
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
        }

        public async Task ReiniciarSteamAsync()
        {
            await CerrarSteamAsync();
            await Task.Delay(1000);
            await IniciarSteamAsync();
        }
    }
}