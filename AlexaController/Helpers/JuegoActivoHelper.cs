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
    /// Localiza el juego que ha lanzado Steam para enfocarlo o detenerlo.
    /// Si no hay ninguno en ejecución, el foco recae sobre la propia ventana de Steam.
    /// </summary>
    public class JuegoActivoHelper
    {
        private readonly ILogger<JuegoActivoHelper> _logger;
        private readonly ProcesosHelper _procesosHelper;
        private readonly SteamHelper _steamHelper;
        private readonly VentanasHelper _ventanasHelper;
        private readonly string[] _patronesRuta;
        private readonly HashSet<string> _excluidos;

        // Procesos que viven bajo las carpetas de Steam pero nunca son el juego
        private static readonly string[] ExcluidosPorDefecto =
        [
            "steam", "steamwebhelper", "steamerrorreporter", "steamservice", "gameoverlayui",
            "UnityCrashHandler32", "UnityCrashHandler64", "crashpad_handler", "CrashReportClient"
        ];

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref uint lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        public JuegoActivoHelper(
            ILogger<JuegoActivoHelper> logger,
            ProcesosHelper procesosHelper,
            SteamHelper steamHelper,
            VentanasHelper ventanasHelper,
            IConfiguration config)
        {
            _logger = logger;
            _procesosHelper = procesosHelper;
            _steamHelper = steamHelper;
            _ventanasHelper = ventanasHelper;

            _patronesRuta = config.GetSection("PatronesRutaJuegos").Get<string[]>()
                ?? [@"steamapps\common"];
            _excluidos = new HashSet<string>(
                ExcluidosPorDefecto.Concat(config.GetSection("ProcesosJuegoExcluidos").Get<string[]>() ?? []),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Trae al frente la ventana del juego lanzado por Steam. Si no hay ninguno,
        /// enfoca la ventana de Steam.
        /// </summary>
        public async Task EnfocarJuegoAsync()
        {
            var juego = BuscarProcesoJuego();
            if (juego is null)
            {
                _logger.LogInformation("No hay ningún juego en ejecución. Enfocando Steam.");
                await _steamHelper.EnfocarSteamAsync();
                return;
            }

            using (juego)
            {
                _logger.LogInformation("Juego detectado: {Nombre} (PID {PID}).", juego.ProcessName, juego.Id);
                await _ventanasHelper.TraerAlFrenteAsync(juego.MainWindowHandle, juego.ProcessName);
            }
        }

        /// <summary>
        /// Cierra el juego lanzado por Steam (y sus procesos hijos) y devuelve el foco a Steam.
        /// </summary>
        public async Task DetenerJuegoAsync()
        {
            var juego = BuscarProcesoJuego();
            if (juego is null)
            {
                _logger.LogInformation("No hay ningún juego en ejecución que detener.");
            }
            else
            {
                using (juego)
                {
                    _logger.LogInformation("Deteniendo juego: {Nombre} (PID {PID}).", juego.ProcessName, juego.Id);
                    await _procesosHelper.KillProcessAndChildrenAsync(juego.Id);
                }
            }

            await _steamHelper.EnfocarSteamAsync();
        }

        /// <summary>
        /// Devuelve el proceso de juego con ventana visible arrancado más recientemente,
        /// o <c>null</c> si no hay ninguno.
        /// </summary>
        private Process? BuscarProcesoJuego()
        {
            Process? candidato = null;
            var arranqueCandidato = DateTime.MinValue;

            foreach (var proceso in Process.GetProcesses())
            {
                var esCandidato = false;
                try
                {
                    if (_excluidos.Contains(proceso.ProcessName)) continue;
                    if (proceso.MainWindowHandle == IntPtr.Zero) continue;
                    if (!EsRutaDeJuego(ObtenerRutaEjecutable(proceso.Id))) continue;

                    var arranque = ObtenerArranque(proceso);
                    if (candidato is not null && arranque <= arranqueCandidato) continue;

                    esCandidato = true;
                    candidato?.Dispose();
                    candidato = proceso;
                    arranqueCandidato = arranque;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "No se pudo inspeccionar el proceso {PID}.", proceso.Id);
                }
                finally
                {
                    if (!esCandidato) proceso.Dispose();
                }
            }

            return candidato;
        }

        private bool EsRutaDeJuego(string? ruta) =>
            ruta is not null &&
            _patronesRuta.Any(patron => ruta.Contains(patron, StringComparison.OrdinalIgnoreCase));

        private static DateTime ObtenerArranque(Process proceso)
        {
            try { return proceso.StartTime; }
            catch { return DateTime.MinValue; }
        }

        /// <summary>
        /// Obtiene la ruta del ejecutable sin abrir el proceso con permisos completos,
        /// para que funcione también con juegos de 32 bits o de otro nivel de integridad.
        /// </summary>
        private static string? ObtenerRutaEjecutable(int pid)
        {
            var handle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (handle == IntPtr.Zero) return null;

            try
            {
                uint capacidad = 1024;
                var buffer = new StringBuilder((int)capacidad);
                return QueryFullProcessImageName(handle, 0, buffer, ref capacidad)
                    ? buffer.ToString()
                    : null;
            }
            finally
            {
                CloseHandle(handle);
            }
        }
    }
}
