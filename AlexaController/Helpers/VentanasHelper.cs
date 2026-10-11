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
using System.Text;

namespace AlexaController.Helpers
{
    /// <summary>
    /// Traslada ventanas al primer plano, sea cual sea el monitor en el que estén,
    /// y cierra las ventanas de usuario para dejar el escritorio limpio.
    /// </summary>
    public class VentanasHelper
    {
        private readonly ILogger<VentanasHelper> _logger;
        private readonly HashSet<string> _procesosExcluidos;
        private readonly int _msEsperaCierre;

        private static readonly IntPtr HWND_TOPMOST = new(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new(-2);
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_SHOWWINDOW = 0x0040;
        private const int SW_RESTORE = 9;
        private const int SW_MINIMIZE = 6;
        private const byte VK_MENU = 0x12;
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const uint WM_CLOSE = 0x0010;
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TOOLWINDOW = 0x00000080;
        private const uint GW_OWNER = 4;
        private const int DWMWA_CLOAKED = 14;

        // Ventanas del propio shell de Windows: nunca deben cerrarse.
        private static readonly HashSet<string> ClasesDelShell = new(StringComparer.OrdinalIgnoreCase)
        {
            "Progman", "WorkerW", "Shell_TrayWnd", "Shell_SecondaryTrayWnd",
            "Button", "TaskListThumbnailWnd", "MultitaskingViewFrame",
            "Windows.UI.Core.CoreWindow", "ForegroundStaging", "XamlExplorerHostIslandWindow"
        };

        // Procesos que nunca deben perder sus ventanas al entrar en modo juegos.
        private static readonly string[] ProcesosExcluidosBase =
        {
            "explorer", "steam", "steamwebhelper", "JoyToKey", "SearchHost", "ShellExperienceHost",
            "StartMenuExperienceHost", "TextInputHost", "ApplicationFrameHost", "SystemSettings"
        };

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")] private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowTextW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassNameW(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowLongW(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")] private static extern bool PostMessageW(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out int pvAttribute, int cbAttribute);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public VentanasHelper(ILogger<VentanasHelper> logger, IConfiguration config)
        {
            _logger = logger;
            _msEsperaCierre = config.GetValue("VentanasEsperaCierreMs", 5000);

            _procesosExcluidos = new HashSet<string>(ProcesosExcluidosBase, StringComparer.OrdinalIgnoreCase)
            {
                Process.GetCurrentProcess().ProcessName
            };
            foreach (var nombre in config.GetSection("VentanasExcluidas").Get<List<string>>() ?? new())
                _procesosExcluidos.Add(nombre);
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

        /// <summary>
        /// Cierra ordenadamente (WM_CLOSE) las ventanas visibles de usuario para dejar el
        /// escritorio limpio antes del modo juegos. Respeta el shell de Windows, Steam,
        /// JoyToKey, la propia aplicación y lo indicado en <c>VentanasExcluidas</c>.
        /// Las que no se cierren a tiempo (por ejemplo, con un diálogo de cambios sin guardar)
        /// se minimizan para que no estorben.
        /// </summary>
        /// <returns>Número de ventanas a las que se pidió el cierre.</returns>
        public async Task<int> CerrarTodasLasVentanasAsync()
        {
            var ventanas = EnumerarVentanasCerrables();
            if (ventanas.Count == 0)
            {
                _logger.LogInformation("No hay ventanas de usuario que cerrar.");
                return 0;
            }

            _logger.LogInformation("Cerrando {Count} ventanas antes del modo juegos...", ventanas.Count);
            foreach (var ventana in ventanas)
            {
                PostMessageW(ventana.Handle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                _logger.LogInformation("Cierre solicitado a '{Titulo}' ({Proceso}).", ventana.Titulo, ventana.Proceso);
            }

            // Comprueba periódicamente en vez de esperar siempre el máximo.
            var restantes = ventanas;
            var espera = Stopwatch.StartNew();
            while (espera.ElapsedMilliseconds < _msEsperaCierre)
            {
                await Task.Delay(250);
                restantes = restantes.Where(v => IsWindow(v.Handle) && IsWindowVisible(v.Handle)).ToList();
                if (restantes.Count == 0) break;
            }

            foreach (var ventana in restantes)
            {
                ShowWindow(ventana.Handle, SW_MINIMIZE);
                _logger.LogWarning("'{Titulo}' ({Proceso}) no se cerró en {Ms} ms; se ha minimizado.",
                    ventana.Titulo, ventana.Proceso, _msEsperaCierre);
            }

            _logger.LogInformation("Cierre de ventanas terminado: {Cerradas} cerradas, {Minimizadas} minimizadas.",
                ventanas.Count - restantes.Count, restantes.Count);

            return ventanas.Count;
        }

        private List<Ventana> EnumerarVentanasCerrables()
        {
            var resultado = new List<Ventana>();

            EnumWindows((hWnd, _) =>
            {
                if (!IsWindowVisible(hWnd)) return true;

                // Solo ventanas de nivel superior propias (sin dueño): descarta diálogos y paletas.
                if (GetWindow(hWnd, GW_OWNER) != IntPtr.Zero) return true;

                if ((GetWindowLongW(hWnd, GWL_EXSTYLE) & WS_EX_TOOLWINDOW) != 0) return true;

                // Las apps UWP suspendidas siguen teniendo ventana visible, pero están "cloaked".
                if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out var oculta, sizeof(int)) == 0 && oculta != 0)
                    return true;

                var titulo = TextoDe(GetWindowTextW, hWnd);
                if (string.IsNullOrWhiteSpace(titulo)) return true;

                if (ClasesDelShell.Contains(TextoDe(GetClassNameW, hWnd))) return true;

                var proceso = NombreDelProceso(hWnd);
                if (proceso is null || _procesosExcluidos.Contains(proceso)) return true;

                resultado.Add(new Ventana(hWnd, titulo, proceso));
                return true;
            }, IntPtr.Zero);

            return resultado;
        }

        private static string TextoDe(Func<IntPtr, StringBuilder, int, int> funcion, IntPtr hWnd)
        {
            var buffer = new StringBuilder(512);
            var longitud = funcion(hWnd, buffer, buffer.Capacity);
            return longitud > 0 ? buffer.ToString() : string.Empty;
        }

        private static string? NombreDelProceso(IntPtr hWnd)
        {
            GetWindowThreadProcessId(hWnd, out var pid);
            if (pid == 0) return null;

            try
            {
                using var proceso = Process.GetProcessById((int)pid);
                return proceso.ProcessName;
            }
            catch
            {
                return null;
            }
        }

        private sealed record Ventana(IntPtr Handle, string Titulo, string Proceso);
    }
}
