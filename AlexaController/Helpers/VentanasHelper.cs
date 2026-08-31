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

using System.Runtime.InteropServices;

namespace AlexaController.Helpers
{
    /// <summary>
    /// Traslada ventanas al primer plano, sea cual sea el monitor en el que estén.
    /// </summary>
    public class VentanasHelper
    {
        private readonly ILogger<VentanasHelper> _logger;

        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;
        private const byte VK_MENU = 0x12;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public VentanasHelper(ILogger<VentanasHelper> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Restaura la ventana si está minimizada y la coloca al frente.
        /// La mantiene TOPMOST durante <paramref name="msTopmost"/> milisegundos para
        /// superar diálogos tardíos y después restaura su comportamiento normal.
        /// </summary>
        public async Task TraerAlFrenteAsync(IntPtr hWnd, string descripcion, int msTopmost = 1500)
        {
            if (hWnd == IntPtr.Zero)
            {
                _logger.LogWarning("No se puede enfocar '{Descripcion}': ventana no válida.", descripcion);
                return;
            }

            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            // TOPMOST garantiza que esté por encima de cualquier diálogo
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

            // Windows bloquea SetForegroundWindow desde procesos en segundo plano;
            // simular una pulsación de ALT desbloquea el cambio de foco.
            keybd_event(VK_MENU, 0, 0, UIntPtr.Zero);
            keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);

            SetForegroundWindow(hWnd);
            _logger.LogInformation("'{Descripcion}' colocado al frente (TOPMOST).", descripcion);

            if (msTopmost > 0)
            {
                await Task.Delay(msTopmost);
                SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
                _logger.LogInformation("'{Descripcion}': TOPMOST eliminado, comportamiento normal restaurado.", descripcion);
            }
        }
    }
}
