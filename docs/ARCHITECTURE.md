# AlexaController — Documentación Técnica

## Índice

1. [Descripción del sistema](#1-descripción-del-sistema)
2. [Arquitectura general](#2-arquitectura-general)
3. [Flujo de una petición de voz](#3-flujo-de-una-petición-de-voz)
4. [API ASP.NET Core](#4-api-aspnet-core)
5. [Función AWS Lambda](#5-función-aws-lambda)
6. [Skill de Alexa](#6-skill-de-alexa)
7. [Aplicación Android (GameController)](#7-aplicación-android-gamecontroller)
8. [Seguridad](#8-seguridad)
9. [Configuración](#9-configuración)
10. [Despliegue y arranque automático](#10-despliegue-y-arranque-automático)
11. [Registro (logs)](#11-registro-logs)
12. [Posibles incidencias](#12-posibles-incidencias)

---

## 1. Descripción del sistema

**AlexaController** es un sistema de automatización de PC por voz. Permite controlar un PC con Windows mediante:

- **Voz** — diciendo comandos a un dispositivo Alexa en español (skill: *"control de equipo"*)
- **Botones** — desde la aplicación Android **GameController**

Acciones disponibles:

| Acción | Descripción |
|--------|-------------|
| `ApagarEquipo` | Apaga el PC (`shutdown /s /t 0`) |
| `ReiniciarEquipo` | Reinicia el PC (`shutdown /r /t 0`) |
| `IniciarSteam` | Lanza Steam (en Big Picture si `SteamBigPicture`) |
| `CerrarSteam` | Cierra Steam (mata árbol de procesos) |
| `ReiniciarSteam` | Cierra y vuelve a abrir Steam |
| `CerrarRetroArch` | Cierra RetroArch y sus hijos |
| `IniciarModoJuegos` | Activa el modo juegos (parar procesos, cerrar ventanas, monitor único, iniciar Steam) |
| `DetenerModoJuegos` | Revierte el modo juegos |
| `SubirVolumen` | Sube el volumen N pasos |
| `BajarVolumen` | Baja el volumen N pasos |
| `Silenciar` | Silencia / activa el sonido |
| `ReconectarMando` | Fuerza una reconexión por software del receptor del mando (PnP) |

Endpoints adicionales (solo desde GameController):

| Endpoint | Método | Descripción |
|----------|--------|-------------|
| `GET /log/Obtener` | GET | Devuelve las entradas del log actual |
| `DELETE /log/Borrar` | DELETE | Vacía el archivo de log |

---

## 2. Arquitectura general

```
┌─────────────────────────────────────────────────────────────────┐
│                        CLIENTE DE VOZ                           │
│   Echo / dispositivo Alexa   ──►  Alexa Cloud                   │
└─────────────────────────────────────────────────────────────────┘
                                          │
                                          ▼ HTTPS
┌─────────────────────────────────────────────────────────────────┐
│                        AWS Lambda (Python 3.13)                 │
│  lambda/AlexaController/src/lambda_function.py                  │
│  - Recibe AlexaRequest (intent name)                            │
│  - Mapea intent → ruta API                                      │
│  - Llama GET /alexa/{accion} con Basic Auth                     │
└─────────────────────────────────────────────────────────────────┘
                                          │
                                          ▼ HTTPS (ngrok)
┌─────────────────────────────────────────────────────────────────┐
│                  ASP.NET Core API  (net10.0-windows)            │
│  AlexaController/                                               │
│  ├── Controllers/AlexaController.cs   GET /alexa/{accion}       │
│  ├── Controllers/LogController.cs     GET|DELETE /log/...       │
│  ├── Helpers/          Lógica de negocio (Steam, volumen, ...)  │
│  ├── Gestores/         Estado del modo juegos                   │
│  └── Seguridad/        Basic Auth handler                       │
│                                                                 │
│  Ejecutándose en http://localhost:5780                          │
└─────────────────────────────────────────────────────────────────┘
                  ▲
                  │ HTTPS (ngrok) / HTTP (LAN)
┌─────────────────────────────────────────────────────────────────┐
│              GameController  (Android, Kotlin)                  │
│  - Botones de acción → POST a /alexa/{accion}                   │
│  - Visor de log → GET /log/Obtener (refresco automático)        │
│  - Ajustes → URL, usuario, contraseña, intervalo, navegación    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 3. Flujo de una petición de voz

```
1. Usuario: "Alexa, dile a control de equipo que inicie Steam"
2. Alexa Cloud: detecta intent → AbrirSteamIntent
3. AWS Lambda: mapea AbrirSteamIntent → "IniciarSteam"
4. Lambda: GET https://<ngrok>/alexa/IniciarSteam  (Basic Auth)
5. API: 200 OK inmediato; Task.Run en background
6. Background: SteamHelper.IniciarSteamAsync() → abre Steam
7. Alexa: responde al usuario con APL + texto de confirmación
```

La API devuelve **200 OK de inmediato** sin esperar a que la acción termine, para no superar el timeout de 8 s de Alexa.

---

## 4. API ASP.NET Core

### Estructura de carpetas

```
AlexaController/
├── Controllers/
│   ├── AlexaController.cs     Endpoints /alexa/{accion}
│   └── LogController.cs       Endpoints /log/Obtener y /log/Borrar
├── Helpers/
│   ├── EquipoHelper.cs        shutdown / restart
│   ├── SteamHelper.cs         iniciar / cerrar / reiniciar Steam (Big Picture)
│   ├── ProcesosHelper.cs      cerrar RetroArch (árbol de procesos)
│   ├── VentanasHelper.cs      enfocar ventanas y cerrarlas todas (Win32)
│   ├── JuegoActivoHelper.cs   localiza, enfoca y detiene el juego lanzado por Steam
│   ├── MonitorHelper.cs       QueryDisplayConfig / SetDisplayConfig (Win32)
│   ├── VolumeHelper.cs        keybd_event VK_VOLUME_* (Win32)
│   ├── JoypadHelper.cs        PowerShell / pnputil – reconexión del mando
│   ├── ProgresoHelper.cs      ventana de progreso del modo juegos (hilo STA propio)
│   └── JuegosHelper.cs        orquesta el modo juegos
├── Gestores/
│   ├── ProgramManager.cs      mata procesos y guarda rutas para relanzarlos
│   └── ServiceManager.cs      para / reactiva servicios de Windows
├── UI/
│   ├── VentanaProgreso.cs     ventana WinForms sin bordes (paso, barra y detalle)
│   └── ProgresoSink.cs        sumidero Serilog que alimenta el detalle de la ventana
├── Seguridad/
│   └── BasicAuthHandler.cs    AuthenticationHandler custom
└── Program.cs                 DI, Serilog, Kestrel, Swagger (dev)
```

### Modo juegos (`JuegosHelper`)

Al **iniciar**:
1. Mata los procesos listados en `Procesos` (guarda sus rutas)
2. Cierra el resto de ventanas de usuario (`VentanasHelper.CerrarTodasLasVentanasAsync`)
3. Guarda topología de monitores actual y cambia a monitor único (`SetDisplayConfig`)
4. Reconecta el mando
5. Inicia Steam (en Big Picture)
6. Detiene los servicios listados en `Servicios`
7. Espera a que aparezca la ventana principal de Steam (`SteamHelper.EsperarVentanaAsync`)

El orden importa: los procesos de la lista se matan **antes** de cerrar ventanas para poder
guardar sus rutas y relanzarlos al salir; los servicios se detienen al final para no
interferir con el arranque de Steam ni con la reinstalación PnP del mando.

Cada paso se anuncia en la ventana de progreso (`ProgresoHelper`), que se cierra en cuanto
Steam está en pantalla; el último paso existe precisamente para eso.

Al **detener** (o al cerrar la aplicación):
1. Cierra Steam
2. Reactiva los servicios
3. Relanza los procesos guardados
4. Restaura la topología de monitores

### Ventana de progreso (`ProgresoHelper` + `UI/`)

El arranque completo ronda el minuto (la reconexión PnP del mando y el cierre de ventanas se
llevan la mayor parte) y hasta que Steam aparece no hay ninguna señal en pantalla. Para
evitar la sensación de bloqueo, `JuegosHelper` abre una ventana que muestra el paso actual
(«Reconectando el mando…»), una barra de progreso y las últimas cinco líneas del registro.

- Vive en un **hilo STA propio** con su bucle de mensajes (`Application.Run`), de modo que se
  repinta aunque el trabajo bloquee al hilo que la maneja. Las actualizaciones se marshalan
  con `BeginInvoke` y nunca propagan excepciones a la operación.
- Es `WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE` y `TopMost`: se ve por encima de todo, no roba el
  foco, no sale en el Alt+Tab y queda fuera de la lista de ventanas que el propio modo juegos
  cierra (que además ya excluye a la propia aplicación).
- El detalle no se instrumenta helper a helper: `UI/ProgresoSink.cs` es un sumidero de Serilog
  que vuelca en la ventana las mismas trazas que van al archivo de log.
- Se recentra al cambiar la topología de monitores (`SystemEvents.DisplaySettingsChanged`),
  que es justo lo que hace el paso 3.
- Se desactiva con `MostrarProgreso: false`.

Requiere WinForms: el proyecto activa `<UseWindowsForms>true</UseWindowsForms>` sobre el SDK
Web (posible porque el TFM es `net10.0-windows`).

### LogController

- **`GET /log/Obtener`** — Lee el archivo de log del día actual (Serilog rolling), parsea las líneas con formato `[HH:mm:ss LVL] mensaje` y las devuelve como JSON estructurado:
  ```json
  {
    "entradas": [
      { "hora": "10:30:00", "nivel": "INF", "mensaje": "AlexaController iniciado…" },
      { "hora": "10:31:00", "nivel": "ERR", "mensaje": "Error al…", "excepcion": "System.Exception:…" }
    ]
  }
  ```
- **`DELETE /log/Borrar`** — Trunca el archivo de log activo (`FileShare.ReadWrite` para no interferir con Serilog `shared: true`).

---

## 5. Función AWS Lambda

Archivo: `lambda/AlexaController/src/lambda_function.py` (Python 3.13)

**Responsabilidades:**
- Recibir el `AlexaRequest` con `intent.name`
- Mapear el nombre del intent al endpoint de la API
- Llamar a `GET https://<API_URL>/alexa/{accion}` con Basic Auth
- Construir una respuesta APL (visual) + `outputSpeech` para Alexa

**Variables de entorno en AWS Lambda:**

| Variable | Descripción |
|----------|-------------|
| `API_URL` | URL pública del túnel ngrok |
| `API_USER` | Usuario Basic Auth |
| `API_PASSWORD` | Contraseña Basic Auth |

---

## 6. Skill de Alexa

Archivo: `AlexaControllerSkill/intents.json`

- Idioma: español
- Invocación: *"control de equipo"*
- 8 intents personalizados más los built-in (`AMAZON.StopIntent`, etc.)

---

## 7. Aplicación Android (GameController)

### Stack

| Capa | Tecnología |
|------|-----------|
| Lenguaje | Kotlin |
| SDK mínimo | Android 8.0 (API 26) |
| SDK objetivo | Android 15 (API 35) |
| DI | Hilt 2.x |
| Configuración | DataStore Preferences |
| HTTP | OkHttp 4.x |
| Navegación | Navigation Component + BottomNavigationView |
| UI | Material Design 3 |
| Logging | Timber |

### Estructura de paquetes

```
com.capicua.gamecontroller
├── data/
│   ├── config/
│   │   ├── AppConfig.kt           Datos de configuración (URL, usuario, contraseña, opciones)
│   │   └── ConfigDataStore.kt     Persistencia con DataStore Preferences
│   └── remote/
│       └── ApiClient.kt           Cliente HTTP (OkHttp), Basic Auth, SSL custom
├── di/
│   ├── ConfigModule.kt            Provee DataStore
│   └── NetworkModule.kt           Provee OkHttpClient
├── domain/model/
│   └── Accion.kt                  Enum de rutas de la API
└── presentation/
    ├── home/
    │   ├── HomeFragment.kt        Botones de acción; navega al log si configurado
    │   └── HomeViewModel.kt       Ejecuta acciones; estados Idle/Cargando/Exito/ExitoIrAlLog/Error
    ├── log/
    │   ├── LogEntrada.kt          Modelo: hora, nivel, mensaje, excepción
    │   ├── LogAdapter.kt          RecyclerView adapter con color por nivel
    │   ├── LogFragment.kt         Visor de log con auto-refresco
    │   └── LogViewModel.kt        Carga/borra log; temporizador de refresco
    ├── settings/
    │   ├── SettingsFragment.kt    Formulario de configuración
    │   └── SettingsViewModel.kt   Guarda config; prueba conexión
    └── about/
        └── AboutFragment.kt       Info de la app y licencia
```

### Configuración persistida (`AppConfig`)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `urlServidor` | String | URL base del servidor (sin `/` final) |
| `usuario` | String | Usuario Basic Auth |
| `contrasena` | String | Contraseña Basic Auth |
| `aceptarCertificadosInvalidos` | Boolean | Ignorar errores SSL (para ngrok free) |
| `intervaloRefrescoLog` | Int | Segundos entre refrescos automáticos del log (mín. 1) |
| `irAlLogAlEjecutar` | Boolean | Navegar automáticamente al visor de log tras ejecutar una acción |

### Visor de log

- El `LogFragment` inicia el refresco automático en `onResume()` y lo para en `onPause()`
- Cada entrada muestra: **borde de color** + **nivel** (INF / WRN / ERR / DBG) + **hora** + **mensaje** + **excepción** (si existe)
- Colores: ERR = rojo, WRN = naranja, INF = azul, DBG = gris
- El botón de borrar solicita confirmación antes de vaciar el log en el servidor

---

## 8. Seguridad

### Basic Auth

- Las credenciales se configuran en `appsettings.json` (`BasicAuth:Username` / `BasicAuth:Password`)
- El `BasicAuthHandler` (custom `AuthenticationHandler`) valida el header `Authorization: Basic <base64>`
- Todos los endpoints de `AlexaController` y `LogController` requieren `[Authorize]`
- `/swagger` está explícitamente excluido del middleware de autenticación

### SSL / ngrok

- La API escucha en HTTP localmente (`http://localhost:5780`); ngrok añade HTTPS y autenticación básica redundante
- La app Android acepta opcionalmente certificados inválidos (para túneles ngrok gratuitos con certificado propio)
- En producción se recomienda usar ngrok con dominio fijo y certificado válido

---

## 9. Configuración

### `AlexaController/appsettings.json`

```json
{
  "BasicAuth": {
    "Username": "tu_usuario",
    "Password": "tu_contraseña"
  },
  "Kestrel": { "Endpoints": { "Http": { "Url": "http://localhost:5780" } } },
  "SteamPath": "C:\\Program Files (x86)\\Steam\\",
  "SteamArgumentos": "-cef-disable-sandbox",
  "SteamBigPicture": true,
  "JoypadFriendlyName": "F710",
  "JoypadIdHardware": "VID_046D&PID_C21F",
  "JoypadReiniciarConcentradorUsb": true,
  "VentanasExcluidas": [],
  "VentanasEsperaCierreMs": 5000,
  "MostrarProgreso": true,
  "VolumenPasos": 3,
  "Procesos": [ "GoogleDriveFS", "OneDrive", "Teams", ... ],
  "Servicios": [ "WSearch", "DiagTrack", ... ]
}
```

| Clave | Descripción |
|---|---|
| `SteamArgumentos` | Argumentos base de `steam.exe` |
| `SteamBigPicture` | Añade `-gamepadui` para arrancar en Big Picture. Si Steam ya está abierto, se le pide el cambio con `steam://open/bigpicture` |
| `JoypadIdHardware` | Fragmento del identificador de hardware del receptor. Permite localizarlo aunque Windows no le haya dado nombre descriptivo por haber fallado la instalación |
| `JoypadReiniciarConcentradorUsb` | Cicla también el concentrador USB padre al reconectar el mando: es lo más parecido a desenchufarlo físicamente y lo único que fuerza una relectura completa de descriptores. Afecta al resto de dispositivos de ese concentrador |
| `Servicios` | Servicios que se detienen en modo juegos. No debe incluir `wuauserv`: si el receptor del mando se reenumera con el modo juegos activo, Windows Update tiene que poder aportar el driver |
| `VentanasExcluidas` | Procesos cuyas ventanas no se cierran al entrar en modo juegos. Se suman a la lista interna (explorador de Windows, Steam, JoyToKey y la propia aplicación) |
| `VentanasEsperaCierreMs` | Espera máxima a que las ventanas se cierren; las que no lo hagan se minimizan |
| `MostrarProgreso` | Muestra la ventana con el avance del modo juegos, que se cierra cuando Steam ya está en pantalla |

### Nota: el receptor F710 y el problema PnP 28

El receptor del **Logitech F710 en modo X** (`USB\VID_046D&PID_C21F`) no empareja por VID/PID:
`C:\Windows\INF\xusb22.inf` sólo declara los identificadores de Microsoft y los IDs compatibles
`USB\MS_COMP_XUSB10` / `XUSB20`, que Windows fabrica leyendo el **descriptor MS OS** del propio
dispositivo (vendor code cacheado en `HKLM\SYSTEM\CurrentControlSet\Control\usbflags\VVVVPPPPRRRR`).

Si esa lectura falla durante la enumeración, el nodo nace sin ningún ID compatible que empareje →
**problema 28** (`CM_PROB_FAILED_INSTALL`) y `ConfigFlags = 0x40` (`CONFIGFLAG_FAILEDINSTALL`).
Con ese bit puesto Windows ya no reintenta la instalación, así que reenumerar o habilitar el
dispositivo no arregla nada: sólo un replug físico (reset de puerto + nodo nuevo) lo resucita.

Por eso `ReconectarMando` hace, en este orden: limpiar `ConfigFlags`, eliminar los nodos fallidos
con `pnputil /remove-device`, borrar la caché `usbflags` del descriptor MS OS, ciclar el
concentrador USB padre y sólo entonces `pnputil /scan-devices`. Si aun así el resultado sigue
siendo problema 28, la solución fiable es **poner el interruptor del mando en modo D**
(`PID_C219`, driver HID de serie, sin descargas) o enchufar el receptor directamente a la placa
en lugar de a un concentrador externo.

### Variables de entorno Lambda

| Variable | Ejemplo |
|----------|---------|
| `API_URL` | `https://xxxx.ngrok-free.app` |
| `API_USER` | `usuario` |
| `API_PASSWORD` | `contraseña` |

---

## 10. Despliegue y arranque automático

### Inicio del servidor

1. La tarea programada `task/Iniciar AlexaController.xml` lanza la API al iniciar sesión en Windows
2. El script `cmd/IniciarNGROK.cmd` abre el túnel ngrok con Basic Auth
3. La API usa un `Mutex` global para evitar instancias duplicadas

### Compilar para producción

```bash
dotnet build --configuration Release
```

El ejecutable se genera en `AlexaController/bin/Release/net10.0-windows/`.

### GameController APK

```bash
cd GameController
./gradlew assembleRelease
```

El APK queda en `GameController/app/build/outputs/apk/release/gamecontroller-<version>.apk`.

---

## 11. Registro (logs)

Serilog escribe en dos destinos simultáneos:

| Destino | Configuración |
|---------|--------------|
| Consola | `[HH:mm:ss LVL] mensaje` |
| Archivo | `logs/AlexaController<YYYYMMDD>.log` (rolling diario, 7 archivos) |

El archivo usa `shared: true` para permitir lecturas y truncado concurrente desde `LogController`.

Nivel mínimo: `Information` (ASP.NET Core internals: `Warning`).

---

## 12. Posibles incidencias

| Síntoma | Causa probable | Solución |
|---------|---------------|----------|
| Alexa dice "no puedo conectar" | ngrok no está corriendo | Ejecutar `cmd/IniciarNGROK.cmd` |
| Error 401 en GameController | Credenciales incorrectas | Revisar Ajustes → usuario/contraseña |
| El modo juegos no detiene todos los procesos | Nombre de proceso incorrecto | Revisar `Procesos` en `appsettings.json` |
| El monitor no cambia al modo juegos | Win32 `SetDisplayConfig` falla | Revisar que `MonitorHelper` detecta el monitor secundario |
| Log vacío en GameController | No hay actividad o log borrado | Ejecutar alguna acción desde Alexa/GameController |
| SSL error en GameController | Certificado del túnel inválido | Activar "Aceptar certificados inválidos" en Ajustes |
