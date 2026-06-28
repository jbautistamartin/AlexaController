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
    public class MonitorHelper
    {
        private readonly ILogger<MonitorHelper> _logger;

        // QDC_DATABASE_CURRENT: devuelve la topología activa guardada en la BD
        private const uint QDC_DATABASE_CURRENT = 0x00000004;

        [DllImport("user32.dll")]
        private static extern int GetDisplayConfigBufferSizes(uint flags, out uint numPathArrayElements, out uint numModeInfoArrayElements);

        [DllImport("user32.dll")]
        private static extern int SetDisplayConfig(uint numPathArrayElements, IntPtr pathArray, uint numModeInfoArrayElements, IntPtr modeInfoArray, uint flags);

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_PATH_INFO
        { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)] public byte[] padding; }

        [StructLayout(LayoutKind.Sequential)]
        private struct DISPLAYCONFIG_MODE_INFO
        { [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)] public byte[] padding; }

        [DllImport("user32.dll")]
        private static extern int QueryDisplayConfig(
            uint flags,
            ref uint numPathArrayElements,
            [Out] DISPLAYCONFIG_PATH_INFO[] pathArray,
            ref uint numModeInfoArrayElements,
            [Out] DISPLAYCONFIG_MODE_INFO[] modeInfoArray,
            out uint currentTopologyId);

        // Flags para SetDisplayConfig (SDC_APPLY + topología)
        private const uint SDC_APPLY = 0x00000080;

        private const uint SDC_TOPOLOGY_INTERNAL = 0x00000001;
        private const uint SDC_TOPOLOGY_CLONE = 0x00000002;
        private const uint SDC_TOPOLOGY_EXTEND = 0x00000004;
        private const uint SDC_TOPOLOGY_EXTERNAL = 0x00000008;
        private const uint SDC_USE_DATABASE_CURRENT = 0x00000F;

        // Valores devueltos por QueryDisplayConfig como topologyId
        private const uint TOPOLOGY_INTERNAL = 0x00000001;

        private const uint TOPOLOGY_CLONE = 0x00000002;
        private const uint TOPOLOGY_EXTEND = 0x00000004;
        private const uint TOPOLOGY_EXTERNAL = 0x00000008;

        public MonitorHelper(ILogger<MonitorHelper> logger)
        {
            _logger = logger;
        }

        public string ObtenerModoActual()
        {
            try
            {
                int result = GetDisplayConfigBufferSizes(QDC_DATABASE_CURRENT, out uint numPaths, out uint numModes);
                if (result != 0)
                {
                    _logger.LogWarning("GetDisplayConfigBufferSizes falló con código {Code}. Asumiendo modo extendido.", result);
                    return "extend";
                }

                var paths = new DISPLAYCONFIG_PATH_INFO[numPaths];
                var modes = new DISPLAYCONFIG_MODE_INFO[numModes];

                result = QueryDisplayConfig(QDC_DATABASE_CURRENT, ref numPaths, paths, ref numModes, modes, out uint topologyId);
                if (result != 0)
                {
                    _logger.LogWarning("QueryDisplayConfig falló con código {Code}. Asumiendo modo extendido.", result);
                    return "extend";
                }

                var modo = topologyId switch
                {
                    TOPOLOGY_INTERNAL => "internal",
                    TOPOLOGY_CLONE => "clone",
                    TOPOLOGY_EXTEND => "extend",
                    TOPOLOGY_EXTERNAL => "external",
                    _ => "extend"
                };

                _logger.LogInformation("Modo de monitores detectado: {Modo} (topologyId={Id})", modo, topologyId);
                return modo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al detectar el modo de monitores. Asumiendo modo extendido.");
                return "extend";
            }
        }

        public void AplicarModo(string modo)
        {
            uint flag = modo switch
            {
                "internal" => SDC_APPLY | SDC_TOPOLOGY_INTERNAL,
                "clone" => SDC_APPLY | SDC_TOPOLOGY_CLONE,
                "extend" => SDC_APPLY | SDC_TOPOLOGY_EXTEND,
                "external" => SDC_APPLY | SDC_TOPOLOGY_EXTERNAL,
                _ => SDC_APPLY | SDC_TOPOLOGY_EXTEND
            };

            int result = SetDisplayConfig(0, IntPtr.Zero, 0, IntPtr.Zero, flag);
            if (result != 0)
                _logger.LogError("SetDisplayConfig({Modo}) falló con código {Code}.", modo, result);
            else
                _logger.LogInformation("Modo de monitores cambiado a: {Modo}", modo);
        }

        public void ActivarSoloMonitorPrincipal() => AplicarModo("internal");

        public void RestaurarModo(string modo) => AplicarModo(modo);
    }
}