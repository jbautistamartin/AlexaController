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
- `AlexaControllerSkill/intents.json` — Alexa skill interaction model (8 custom intents)
- `lambda/AlexaController/src/lambda_function.py` — AWS Lambda handler with APL support

## Key Components

**`Controllers/AlexaController.cs`** — Single controller with `[Authorize]` on all actions. Routes: `GET /alexa/{action}`. Immediately returns HTTP 200 and runs the real work in a background `Task.Run`. The `/swagger` path is explicitly excluded from authentication middleware.

**Endpoints:** `Estado` (returns gaming-mode state JSON), `ApagarEquipo`, `ReiniciarEquipo`, `IniciarSteam`, `CerrarSteam`, `ReiniciarSteam`, `CerrarRetroArch`, `IniciarModoJuegos`, `DetenerModoJuegos`, `SubirVolumen`, `BajarVolumen`, `Silenciar`.

**`Helpers/`** — Each helper wraps a single concern:
- `EquipoHelper` — shutdown/restart via `shutdown /s /t 0` / `shutdown /r /t 0`
- `SteamHelper` — start/stop/restart Steam (kills process tree on close); Steam path from `SteamPath` in config
- `ProcesosHelper` — kill RetroArch (and its child processes)
- `MonitorHelper` — reads and switches Windows display topology (internal/clone/extend/external) via `QueryDisplayConfig` / `SetDisplayConfig` Win32 P/Invoke
- `VolumeHelper` — raises/lowers/mutes system volume via Win32 `keybd_event` (VK_VOLUME_UP/DOWN/MUTE); step count from `VolumenPasos` in config (default: 3)
- `JuegosHelper` — orchestrates "gaming mode": saves monitor topology, switches to single monitor, stops background apps + disables Windows services (lists from `Procesos`/`Servicios` in `appsettings.json`), starts Steam; reverses everything on stop

**`Gestores/`** — Stateful managers used only by `JuegosHelper`:
- `ProgramManager` — kills background apps and remembers their paths to restart them
- `ServiceManager` — disables Windows services and tracks them to re-enable later
- `StateManager` — persists gaming-mode state (active flag, stopped processes, disabled services, previous monitor topology) to a JSON file so it survives app restarts; path configured via `EstadoFilePath` in `appsettings.json` (default: `estado_modo_juegos.json`)

**`Seguridad/BasicAuthHandler.cs`** — Custom `AuthenticationHandler` that validates Base64 credentials from the `Authorization` header against `BasicAuth:Username` / `BasicAuth:Password` in `appsettings.json`.

## Configuration

`appsettings.json` holds all runtime config:
- `BasicAuth:Username` / `BasicAuth:Password` — API credentials (also set in ngrok `--basic-auth`)
- `Kestrel:Endpoints:Http:Url` — hardcoded to `http://localhost:5780`
- `EstadoFilePath` — path for the gaming-mode state JSON file (default: `estado_modo_juegos.json`)
- `SteamPath` — path to Steam installation (default: `C:\Program Files (x86)\Steam\`)
- `VolumenPasos` — number of key presses per volume up/down command (default: `3`)
- `Procesos` — JSON array of process names to kill when entering gaming mode
- `Servicios` — JSON array of Windows service names to stop/disable when entering gaming mode

All DI registrations are in `Program.cs` (all Singletons). Serilog writes to console + daily rolling file at `logs/AlexaController.log`.

## Dependencies

- `Serilog.AspNetCore` + `Serilog.Sinks.File` — structured logging
- `System.Management` — Windows process management (killing process trees)
- `System.ServiceProcess.ServiceController` — enabling/disabling Windows services
- `Swashbuckle.AspNetCore` — Swagger UI (Development only, at `/swagger`)
