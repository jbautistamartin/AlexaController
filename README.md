# AlexaController

[![Última versión](https://img.shields.io/github/v/release/jbautistamartin/AlexaController?label=descargar&color=2E7D32)](https://github.com/jbautistamartin/AlexaController/releases/latest)
[![Licencia](https://img.shields.io/badge/licencia-LGPL--2.1-blue)](LICENSE)
[![Donar con PayPal](https://img.shields.io/badge/Donar-PayPal-00457C?logo=paypal&logoColor=white)](https://paypal.me/jbautistamartin)

Sistema de automatización de PC mediante comandos de voz a través de un skill de Alexa en español, o desde la aplicación Android **GameController**.

---

## ¿Qué hace?

Permite controlar un PC de forma remota usando la voz o botones en el móvil:

- Apagar y reiniciar el equipo
- Iniciar, cerrar y reiniciar Steam
- Cerrar RetroArch
- Activar y desactivar el **modo juegos** — cierra las ventanas abiertas, cambia a monitor único, detiene procesos y servicios en segundo plano, inicia Steam en Big Picture, con una ventana que muestra el avance hasta que Steam está listo
- Subir, bajar y silenciar el volumen
- Reconectar el mando
- Ver y borrar el **log del servidor** desde el móvil

---

## Arquitectura

```
Voz  →  Alexa Cloud  →  AWS Lambda  →  API ASP.NET Core (PC local)
Móvil (GameController Android)      →  API ASP.NET Core (PC local)
```

1. El usuario da un comando de voz o pulsa un botón en la app.
2. Alexa Cloud lo procesa y llama a la función **AWS Lambda** (`lambda/AlexaController/src/lambda_function.py`).
3. Lambda traduce el intent al endpoint y llama a `GET /alexa/{accion}` con autenticación básica.
4. La **API ASP.NET Core** (`.NET 10`, ejecutándose en el PC) recibe la petición, responde inmediatamente con HTTP 200 y ejecuta la acción en segundo plano.
5. La app Android llama directamente a la misma API; también puede consultar y borrar el log.

La API local se expone a internet mediante un túnel **ngrok**.

Consulta [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) para la documentación técnica completa.

---

## Estructura del proyecto

```
AlexaController/          API ASP.NET Core (.NET 10, Windows)
  Controllers/            Endpoints /alexa/{accion} y /log/...
  Helpers/                Lógica de cada acción (Steam, volumen, monitor...)
  Gestores/               Estado del modo juegos (procesos, servicios, monitor)
  UI/                     Ventana de progreso del modo juegos (WinForms)
  Seguridad/              Autenticación básica
AlexaControllerSkill/     Modelo de interacción del skill de Alexa
GameController/           Aplicación Android (Kotlin, Material Design 3)
lambda/                   Función AWS Lambda (Python 3.13)
cmd/                      Scripts de arranque (ngrok, etc.)
task/                     Tarea del Programador de Windows
docs/                     Documentación técnica
```

---

## Descargas

Los binarios de cada versión están en la
**[página de releases](https://github.com/jbautistamartin/AlexaController/releases/latest)**.

| Artefacto | Qué es | Dónde va |
|---|---|---|
| `AlexaController-<version>-win-x64.zip` | API .NET, autocontenida | el PC que se controla |
| `gamecontroller-<version>.apk` | Aplicación Android firmada | el móvil |
| `lambda-alexacontroller-<version>.zip` | Manejador del skill | AWS Lambda (Python 3.13) |
| `intents-<version>.json` | Modelo de interacción | Alexa Developer Console |

La API va **autocontenida**: se descomprime y funciona, sin instalar ningún runtime de .NET.
Las credenciales de `appsettings.json` vienen vacías a propósito y hay que rellenarlas — ver
[Configuración](#configuración).

> El ejecutable **pide elevación** (UAC). No es un descuido: sin privilegios de administrador
> no se puede reinstalar el receptor del mando ni detener servicios de Windows, que es la mitad
> del modo juegos.

Las cuatro piezas se despliegan en cuatro sitios distintos y **el orden importa**: los pasos
están en las [notas del release](https://github.com/jbautistamartin/AlexaController/releases/latest).

---

## Requisitos

Para **usar** los binarios del release:

- Windows 10/11
- Cuenta de AWS con una función Lambda configurada
- Skill de Alexa publicado en Alexa Developer Console
- [ngrok](https://ngrok.com/) con un túnel fijo

Para **compilar** desde el código:

- .NET 10 SDK
- JDK 21 y Android Studio (GameController)
- [GitHub CLI](https://cli.github.com/) (`gh`), solo si se va a publicar un release

---

## Configuración

### 1. API local (`AlexaController/appsettings.json`)

```json
{
  "BasicAuth": {
    "Username": "tu_usuario",
    "Password": "tu_contraseña"
  },
  "SteamPath": "C:\\Program Files (x86)\\Steam\\",
  "SteamBigPicture": true,
  "VentanasExcluidas": [],
  "MostrarProgreso": true,
  "VolumenPasos": 3
}
```

`SteamBigPicture` arranca Steam con `-gamepadui`. `VentanasExcluidas` lista los procesos cuyas
ventanas **no** se cierran al entrar en modo juegos (además del explorador de Windows, Steam,
JoyToKey y la propia aplicación). `MostrarProgreso` muestra la ventana con el avance del modo
juegos, que desaparece en cuanto Steam está en pantalla. El resto de claves están documentadas en
[`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

Las credenciales deben coincidir con las configuradas en ngrok (`--basic-auth "usuario:contraseña"`).

### 2. Función Lambda (variables de entorno)

En la consola de AWS Lambda, **Configuración → Variables de entorno**:

| Variable | Descripción |
|---|---|
| `API_URL` | URL pública del túnel ngrok (ej: `https://xxxx.ngrok-free.app`) |
| `API_USER` | Usuario de autenticación básica |
| `API_PASSWORD` | Contraseña de autenticación básica |

### 3. ngrok

```cmd
ngrok http 5780 --basic-auth "usuario:contraseña"
```

El script `cmd/IniciarNGROK.cmd` automatiza esto. No se versiona porque lleva las
credenciales y el dominio reservado: se parte de `cmd/IniciarNGROK.cmd.example`, se
copia sin el `.example` y se rellenan los tres valores.

### 4. GameController (app Android)

En la pantalla de **Ajustes** de la app, configurar:

- **URL del servidor** — URL pública del túnel ngrok
- **Usuario / Contraseña** — credenciales Basic Auth
- **Aceptar certificados HTTPS inválidos** — activar si el certificado de ngrok da error
- **Refresco del log (segundos)** — intervalo de auto-refresco del visor de log (por defecto: 5 s)
- **Ir al log al ejecutar una acción** — navega automáticamente al visor de log tras pulsar cualquier botón

---

## Ejecución

```bash
# Desarrollo
dotnet run --project AlexaController

# Producción (compilar)
dotnet build --configuration Release
```

Los binarios tal como se publican en el release los genera `release.ps1`:

```powershell
.elease.ps1 -SoloCompilar   # compila las tres piezas y las deja en dist/
.elease.ps1                 # además etiqueta y publica el release en GitHub
```

Detalles en [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md#generar-un-release).

La API queda disponible en `http://localhost:5780`. El túnel ngrok la expone públicamente.

En producción, una tarea del Programador de tareas de Windows arranca la API automáticamente
al iniciar sesión. La plantilla es `task/Iniciar AlexaController.xml.example`: se copia sin el
`.example`, se sustituyen el usuario de Windows y las rutas, y se importa con
`schtasks /create /tn "Iniciar AlexaController" /xml "Iniciar AlexaController.xml"`.

La tarea tiene que correr con **privilegios máximos**: el ejecutable declara
`requireAdministrator`, porque sin elevación no se puede reinstalar el receptor del mando.

---

## GameController — Visor de log

La pantalla **Log** de la app Android muestra las entradas del log del servidor en tiempo real:

- **Auto-refresco** configurable (chip "Auto" en la barra superior)
- Refresco manual con el botón de actualizar
- **Color por nivel**: rojo (ERR), naranja (WRN), azul (INF), gris (DBG)
- Botón de **borrar** con confirmación — vacía el log en el servidor
- Scroll automático a la entrada más reciente

---

## Apoyar el proyecto

AlexaController y GameController son gratuitos y de código abierto. Si te resultan útiles, puedes invitarme a un café:

[![Donar con PayPal](https://img.shields.io/badge/Donar-PayPal-00457C?logo=paypal&logoColor=white)](https://paypal.me/jbautistamartin)

El enlace también está dentro de GameController, en la pestaña **Acerca de** («Invitar a un café»). Es completamente opcional: la aplicación no muestra avisos ni limita ninguna función.

---

## Licencia

Distribuido bajo la licencia **GNU Lesser General Public License v2.1**.  
Consulta el archivo [LICENSE](LICENSE) para más detalles.
