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
    /// <summary>
    /// Fuerza una reconexión "por software" del mando, equivalente a desenchufar
    /// y volver a enchufar el receptor USB.
    /// </summary>
    /// <remarks>
    /// El receptor del F710 en modo X no empareja por VID/PID: <c>xusb22.inf</c> sólo declara los
    /// identificadores de Microsoft y los IDs compatibles <c>USB\MS_COMP_XUSB10/20</c>, que Windows
    /// genera leyendo el descriptor MS OS del propio dispositivo. Si esa lectura falla, el nodo nace
    /// sin driver (problema 28) y queda marcado con <c>CONFIGFLAG_FAILEDINSTALL</c>, con lo que
    /// Windows ya no reintenta la instalación por mucho que se reenumere. Por eso, además de
    /// eliminar el nodo, aquí se limpian ese marcador y la caché del descriptor MS OS
    /// (<c>Control\usbflags</c>) antes de volver a escanear.
    /// </remarks>
    public class JoypadHelper
    {
        private readonly ILogger<JoypadHelper> _logger;
        private readonly string _friendlyName;
        private readonly string _idHardware;
        private readonly bool _reiniciarConcentrador;

        public JoypadHelper(ILogger<JoypadHelper> logger, IConfiguration config)
        {
            _logger = logger;
            _friendlyName = config["JoypadFriendlyName"] ?? "F710";
            _idHardware = config["JoypadIdHardware"] ?? "VID_046D&PID_C21F";
            _reiniciarConcentrador = config.GetValue("JoypadReiniciarConcentradorUsb", false);
        }

        // Marcadores sustituidos antes de ejecutar el script.
        private const string Plantilla = """
            $ErrorActionPreference = 'Continue'
            $patron   = '*{PATRON}*'
            $patronId = '*{PATRON_ID}*'
            $ciclaHub = {CICLAR_HUB}

            # Sin permisos de administrador ninguna operacion PnP funciona, y todas fallan
            # en silencio: es preferible abortar con un error claro.
            $identidad = [Security.Principal.WindowsIdentity]::GetCurrent()
            $principal = New-Object Security.Principal.WindowsPrincipal($identidad)
            if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
                Write-Error "Se requieren permisos de administrador para reconectar el mando (usuario: $($identidad.Name))."
                exit 3
            }

            function Get-Propiedad($id, $clave) {
                try { (Get-PnpDeviceProperty -InstanceId $id -KeyName $clave -ErrorAction Stop).Data }
                catch { $null }
            }

            function Get-Problema($id) {
                $p = Get-Propiedad $id 'DEVPKEY_Device_ProblemCode'
                if ($null -eq $p) { -1 } else { $p }
            }

            $devs = @(Get-PnpDevice | Where-Object { $_.FriendlyName -like $patron -or $_.InstanceId -like $patronId })
            if ($devs.Count -eq 0) { Write-Error "Dispositivo no encontrado: $patron / $patronId"; exit 2 }

            $padres    = @()
            $cachesUsb = @()

            foreach ($d in $devs) {
                $problema = Get-Problema $d.InstanceId
                Write-Host "Encontrado: '$($d.FriendlyName)' [$($d.InstanceId)] Estado=$($d.Status) Clase='$($d.Class)' Problema=$problema"

                $padre = Get-Propiedad $d.InstanceId 'DEVPKEY_Device_Parent'
                if ($padre) { $padres += $padre }

                # Claves de la cache del descriptor MS OS: VVVVPPPPRRRR por cada hardware ID.
                foreach ($hw in @(Get-Propiedad $d.InstanceId 'DEVPKEY_Device_HardwareIds')) {
                    if ($hw -match 'VID_([0-9A-Fa-f]{4})&PID_([0-9A-Fa-f]{4})&REV_([0-9A-Fa-f]{4})') {
                        $cachesUsb += ($Matches[1] + $Matches[2] + $Matches[3]).ToUpper()
                    }
                }

                # CONFIGFLAG_FAILEDINSTALL (0x40): mientras este puesto, Windows da por
                # imposible la instalacion y no vuelve a intentarla al reenumerar.
                $clave = "HKLM:\SYSTEM\CurrentControlSet\Enum\$($d.InstanceId)"
                $flags = (Get-ItemProperty -Path $clave -Name ConfigFlags -ErrorAction SilentlyContinue).ConfigFlags
                if ($flags) {
                    try {
                        Set-ItemProperty -Path $clave -Name ConfigFlags -Value 0 -Type DWord -ErrorAction Stop
                        Write-Host ("  -> ConfigFlags 0x{0:X} limpiado." -f $flags)
                    }
                    catch {
                        Write-Host ("  AVISO: no se pudo limpiar ConfigFlags 0x{0:X}: {1}" -f $flags, $_.Exception.Message)
                    }
                }

                if ($d.Status -eq 'OK' -and $problema -eq 0) {
                    # El dispositivo funciona: basta con un ciclo deshabilitar/habilitar.
                    try {
                        Disable-PnpDevice -InstanceId $d.InstanceId -Confirm:$false -ErrorAction Stop
                        Start-Sleep -Seconds 2
                        Enable-PnpDevice -InstanceId $d.InstanceId -Confirm:$false -ErrorAction Stop
                        Write-Host "  -> Ciclo deshabilitar/habilitar aplicado."
                    }
                    catch {
                        Write-Host "  AVISO: fallo el ciclo deshabilitar/habilitar: $($_.Exception.Message)"
                    }
                }
                else {
                    # Instalacion fallida o dispositivo fantasma: habilitar no sirve de nada,
                    # hay que eliminar el nodo para que Windows lo vuelva a enumerar e instalar.
                    $salida = & pnputil.exe /remove-device "$($d.InstanceId)" 2>&1
                    $codigo = $LASTEXITCODE
                    if ($codigo -eq 0) {
                        Write-Host "  -> Nodo eliminado (pnputil /remove-device)."
                    }
                    else {
                        Write-Host "  AVISO: pnputil /remove-device devolvio $codigo : $($salida -join ' ')"
                    }
                }
            }

            # Cache del descriptor MS OS: si Windows guardo una respuesta incompleta, el receptor
            # se reenumera sin el ID compatible USB\MS_COMP_XUSB10 y ninguna INF llega a emparejar.
            # Borrarla obliga a Windows a volver a preguntarle al dispositivo.
            foreach ($c in ($cachesUsb | Select-Object -Unique)) {
                $clave = "HKLM:\SYSTEM\CurrentControlSet\Control\usbflags\$c"
                if (Test-Path $clave) {
                    try {
                        Remove-Item -Path $clave -Recurse -Force -ErrorAction Stop
                        Write-Host "Cache del descriptor MS OS borrada: usbflags\$c"
                    }
                    catch {
                        Write-Host "AVISO: no se pudo borrar usbflags\$c : $($_.Exception.Message)"
                    }
                }
            }

            if ($ciclaHub) {
                foreach ($p in ($padres | Select-Object -Unique)) {
                    Write-Host "Reiniciando concentrador USB padre: $p"
                    try {
                        Disable-PnpDevice -InstanceId $p -Confirm:$false -ErrorAction Stop
                        Start-Sleep -Seconds 3
                    }
                    catch {
                        Write-Host "  AVISO: no se pudo deshabilitar el concentrador: $($_.Exception.Message)"
                    }
                    finally {
                        # Pase lo que pase hay que volver a habilitarlo: dejarlo apagado se
                        # llevaria por delante al resto de dispositivos del concentrador.
                        $habilitado = $false
                        foreach ($intento in 1..3) {
                            try {
                                Enable-PnpDevice -InstanceId $p -Confirm:$false -ErrorAction Stop
                                $habilitado = $true
                                break
                            }
                            catch { Start-Sleep -Seconds 2 }
                        }
                        if ($habilitado) { Write-Host "  -> Concentrador reiniciado." }
                        else { Write-Host "  AVISO: el concentrador $p NO se pudo volver a habilitar." }
                    }
                }
            }

            # Fuerza la re-enumeracion del arbol PnP (equivale a "Buscar cambios de hardware").
            & pnputil.exe /scan-devices | Out-Null
            Start-Sleep -Seconds 3

            $final = @(Get-PnpDevice -PresentOnly | Where-Object { $_.FriendlyName -like $patron -or $_.InstanceId -like $patronId })
            if ($final.Count -eq 0) {
                Write-Host "RESULTADO: el dispositivo no ha vuelto a aparecer tras la re-enumeracion."
            }
            foreach ($d in $final) {
                $problema = Get-Problema $d.InstanceId
                Write-Host "RESULTADO: '$($d.FriendlyName)' [$($d.InstanceId)] Estado=$($d.Status) Clase='$($d.Class)' Problema=$problema"
                if ($problema -eq 28) {
                    Write-Host "  AVISO: problema 28 = Windows no ha emparejado ninguna INF con este receptor."
                    if (-not $ciclaHub) {
                        Write-Host "  AVISO: prueba a activar JoypadReiniciarConcentradorUsb, a enchufar el receptor directo a la placa o a pasar el mando a modo D."
                    }
                    else {
                        Write-Host "  AVISO: solo queda el replug fisico, enchufarlo directo a la placa o pasar el mando a modo D (PID_C219, driver HID de serie)."
                    }
                }
            }
            """;

        public async Task ReconectarMandoAsync()
        {
            _logger.LogInformation("Reconectando mando '{Nombre}'...", _friendlyName);

            var script = Plantilla
                .Replace("{PATRON}", Sanear(_friendlyName))
                .Replace("{PATRON_ID}", Sanear(_idHardware))
                .Replace("{CICLAR_HUB}", _reiniciarConcentrador ? "$true" : "$false");

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

            var lecturaSalida = proceso.StandardOutput.ReadToEndAsync();
            var lecturaError = proceso.StandardError.ReadToEndAsync();
            await proceso.WaitForExitAsync();

            var stdout = (await lecturaSalida).Trim();
            var stderr = (await lecturaError).Trim();

            foreach (var linea in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries))
            {
                var texto = linea.TrimEnd();
                if (texto.Contains("AVISO", StringComparison.Ordinal))
                    _logger.LogWarning("[mando] {Linea}", texto);
                else
                    _logger.LogInformation("[mando] {Linea}", texto);
            }

            if (proceso.ExitCode != 0)
                _logger.LogError("Error al reconectar el mando (código {Code}): {Error}", proceso.ExitCode, stderr);
            else if (stderr.Length > 0)
                _logger.LogWarning("Reconexión del mando con avisos: {Error}", stderr);
            else
                _logger.LogInformation("Reconexión del mando completada.");
        }

        /// <summary>Escapa el valor para evitar inyección en el script de PowerShell.</summary>
        private static string Sanear(string valor) =>
            valor.Replace("'", "''").Replace("*", "").Replace("`", "");
    }
}
