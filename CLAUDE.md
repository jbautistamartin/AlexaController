# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build
dotnet build

# Run (development)
dotnet run --project AlexaController

# Build release
dotnet build --configuration Release
```

The app runs on `http://localhost:5780`. In production, it is exposed via a fixed ngrok tunnel (URL configured in `cmd/IniciarNGROK.cmd`) started by that same script. A Windows Task Scheduler task (`task/Iniciar AlexaController.xml`) auto-starts it on user logon.

## Architecture

This is a voice-controlled PC automation system. An Alexa skill (in Spanish, invocation: "control de equipo") sends commands to an ASP.NET Core (.NET 10, `net10.0-windows`) API running locally.

**Request flow:**
1. Voice command → Alexa Cloud
2. Alexa Cloud → AWS Lambda (`lambda/AlexaController/src/lambda_function.py`, Python 3.13)
3. Lambda maps intent name to API action, calls `GET /alexa/{action}` with Basic Auth
4. ASP.NET Core controller spawns a `Task.Run` background task and returns immediately
5. Background task executes system commands (shutdown, process kill, service toggle)

**Core projects:**
- `AlexaController/` — .NET 10 API (the main project)
- `AlexaControllerSkill/intents.json` — Alexa skill interaction model (custom intents)
- `lambda/AlexaController/src/lambda_function.py` — AWS Lambda handler with APL support

## Key Components

**`Controllers/AlexaController.cs`** — Single controller with `[Authorize]` on all actions. Routes: `GET /alexa/{action}`. Immediately returns HTTP 200 and runs the real work in a background `Task.Run`. The `/swagger` path is explicitly excluded from authentication middleware.

**Endpoints:** `ApagarEquipo`, `ReiniciarEquipo`, `IniciarSteam`, `CerrarSteam`, `ReiniciarSteam`, `CerrarRetroArch`, `IniciarModoJuegos`, `DetenerModoJuegos`, `EnfocarJuego`, `DetenerJuego`, `SubirVolumen`, `BajarVolumen`, `Silenciar`.

**`Helpers/`** — Each helper wraps a single concern:
- `EquipoHelper` — shutdown/restart via `shutdown /s /t 0` / `shutdown /r /t 0`
- `SteamHelper` — start/stop/restart Steam (kills process tree on close, `steamwebhelper` included); Steam path from `SteamPath`, launch args from `SteamArgumentos`, and `-gamepadui` (Big Picture) appended when `SteamBigPicture` is true. If Steam is already running, Big Picture is requested via `steam://open/bigpicture`
- `ProcesosHelper` — kill RetroArch (and its child processes)
- `VentanasHelper` — brings a window to the foreground on any monitor (restore + TOPMOST + `SetForegroundWindow`); used by `SteamHelper` and `JuegoActivoHelper`. Also `CerrarTodasLasVentanasAsync`: posts `WM_CLOSE` to every visible, non-cloaked, unowned top-level window (skipping shell classes and the processes in `VentanasExcluidas` plus a built-in list) and minimizes whatever survives `VentanasEsperaCierreMs`
- `JoypadHelper` — software "replug" of the gamepad receiver. Requires elevation (it aborts with a clear error otherwise). For every device matching `JoypadFriendlyName` or `JoypadIdHardware` it: clears `ConfigFlags` (`CONFIGFLAG_FAILEDINSTALL`, which otherwise stops Windows from ever retrying the install), disable/enables healthy instances and `pnputil /remove-device`s the failed ones, deletes the MS OS descriptor cache under `Control\usbflags\VVVVPPPPRRRR` (a stale entry is what leaves the F710 without the `USB\MS_COMP_XUSB10` compatible ID that `xusb22.inf` matches on), optionally cycles the parent USB hub (`JoypadReiniciarConcentradorUsb`, re-enabled in a `finally` with retries so the hub can never be left off), then `pnputil /scan-devices` and logs the resulting PnP problem code. See "el receptor F710 y el problema PnP 28" in `docs/ARCHITECTURE.md`
- `JuegoActivoHelper` — finds the game Steam launched (process whose executable path matches `PatronesRutaJuegos` (`steamapps\common`, `C:\Games`), with a visible window and the most recent start time) to focus it (`EnfocarJuego`) or kill it and return focus to Steam (`DetenerJuego`); falls back to focusing Steam when no game is running
- `MonitorHelper` — reads and switches Windows display topology (internal/clone/extend/external) via `QueryDisplayConfig` / `SetDisplayConfig` Win32 P/Invoke
- `VolumeHelper` — raises/lowers/mutes system volume via Win32 `keybd_event` (VK_VOLUME_UP/DOWN/MUTE); step count from `VolumenPasos` in config (default: 3)
- `JuegosHelper` — orchestrates "gaming mode": stops background apps (list from `Procesos`, paths saved so they can be relaunched), closes every remaining window, saves monitor topology and switches to single monitor, reconnects the gamepad, starts Steam, then disables Windows services (`Servicios`); reverses everything on stop. Killing the `Procesos` list must stay *before* closing windows so their paths are captured for the restore

**`Gestores/`** — Managers used only by `JuegosHelper`:
- `ProgramManager` — kills background apps and returns their paths so they can be restarted
- `ServiceManager` — stops Windows services and returns their names so they can be re-enabled

**`Seguridad/BasicAuthHandler.cs`** — Custom `AuthenticationHandler` that validates Base64 credentials from the `Authorization` header against `BasicAuth:Username` / `BasicAuth:Password` in `appsettings.json`.

## Configuration

`appsettings.json` holds all runtime config:
- `BasicAuth:Username` / `BasicAuth:Password` — API credentials (also set in ngrok `--basic-auth`)
- `Kestrel:Endpoints:Http:Url` — hardcoded to `http://localhost:5780`
- `SteamPath` — path to Steam installation (default: `C:\Program Files (x86)\Steam\`)
- `SteamArgumentos` — base steam.exe arguments (default: `-cef-disable-sandbox`)
- `SteamBigPicture` — launch Steam in Big Picture / `-gamepadui` (default: `true`)
- `JoypadFriendlyName` — friendly-name fragment identifying the gamepad receiver (default: `F710`)
- `JoypadIdHardware` — hardware-id fragment identifying the receiver, used as well as the friendly name so a failed-install node with no name is still found (default: `VID_046D&PID_C21F`)
- `JoypadReiniciarConcentradorUsb` — also power-cycle the receiver's parent USB hub on reconnect (default: `true`; affects every device on that hub, but it is the only thing that forces a full descriptor re-read)
- `VentanasExcluidas` — process names whose windows survive gaming mode (added to the built-in list)
- `VentanasEsperaCierreMs` — grace period before unclosed windows get minimized (default: `5000`)
- `VolumenPasos` — number of key presses per volume up/down command (default: `3`)
- `PatronesRutaJuegos` — path fragments that identify a game executable (default: `steamapps\common`, `C:\Games`)
- `ProcesosJuegoExcluidos` — extra process names never treated as the active game (added to the built-in list)
- `Procesos` — JSON array of process names to kill when entering gaming mode
- `Servicios` — JSON array of Windows service names to stop/disable when entering gaming mode. Must not include `wuauserv`: if the receiver re-enumerates while gaming mode is on, Windows Update has to be able to supply the driver

All DI registrations are in `Program.cs` (all Singletons). Serilog writes to console + daily rolling file at `logs/AlexaController.log`.

## Dependencies

- `Serilog.AspNetCore` + `Serilog.Sinks.File` — structured logging
- `System.Management` — Windows process management (killing process trees)
- `System.ServiceProcess.ServiceController` — enabling/disabling Windows services
- `Swashbuckle.AspNetCore` — Swagger UI (Development only, at `/swagger`)
