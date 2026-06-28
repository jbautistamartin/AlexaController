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
using System.Text;

namespace AlexaController.Helpers
{
    public class JoypadHelper
    {
        private readonly ILogger<JoypadHelper> _logger;
        private readonly string _friendlyName;

        public JoypadHelper(ILogger<JoypadHelper> logger, IConfiguration config)
        {
            _logger = logger;
            _friendlyName = config["JoypadFriendlyName"] ?? "F710";
        }

        public async Task ReconectarMandoAsync()
        {
            _logger.LogInformation("Reconectando mando '{Nombre}'...", _friendlyName);

            // Escapa el nombre para evitar inyección en el script de PowerShell
            var nombreSeguro = _friendlyName.Replace("'", "''").Replace("*", "").Replace("`", "");

            var script = $@"
$device = Get-PnpDevice | Where-Object {{ $_.FriendlyName -like '*{nombreSeguro}*' }} | Select-Object -First 1
if (-not $device) {{ Write-Error 'Dispositivo no encontrado: {nombreSeguro}'; exit 1 }}
Disable-PnpDevice -InstanceId $device.InstanceId -Confirm:$false
Start-Sleep -Seconds 2
Enable-PnpDevice -InstanceId $device.InstanceId -Confirm:$false
Write-Host ""Mando reconectado: $($device.FriendlyName)""
";

            var encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));

            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NonInteractive -ExecutionPolicy Bypass -EncodedCommand {encodedCommand}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proceso = Process.Start(startInfo)
                ?? throw new InvalidOperationException("No se pudo iniciar powershell.exe.");

            var stdout = await proceso.StandardOutput.ReadToEndAsync();
            var stderr = await proceso.StandardError.ReadToEndAsync();
            await proceso.WaitForExitAsync();

            if (proceso.ExitCode != 0)
                _logger.LogError("Error al reconectar el mando (código {Code}): {Error}", proceso.ExitCode, stderr.Trim());
            else
                _logger.LogInformation("Mando reconectado correctamente. {Output}", stdout.Trim());
        }
    }
}