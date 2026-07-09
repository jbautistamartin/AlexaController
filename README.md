# AlexaController

Sistema de automatización de PC mediante comandos de voz a través de un skill de Alexa en español, o desde la aplicación Android **GameController**.

---

## ¿Qué hace?

Permite controlar un PC de forma remota usando la voz o botones en el móvil:

- Apagar y reiniciar el equipo
- Iniciar, cerrar y reiniciar Steam
- Cerrar RetroArch
- Activar y desactivar el **modo juegos** — cambia a monitor único, detiene procesos y servicios en segundo plano, inicia Steam
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
  Seguridad/              Autenticación básica
AlexaControllerSkill/     Modelo de interacción del skill de Alexa
GameController/           Aplicación Android (Kotlin, Material Design 3)
lambda/                   Función AWS Lambda (Python 3.13)
cmd/                      Scripts de arranque (ngrok, etc.)
task/                     Tarea del Programador de Windows
docs/                     Documentación técnica
```

---

## Requisitos

- Windows 10/11
- .NET 10 SDK
- Cuenta de AWS con una función Lambda configurada
- Skill de Alexa publicado en Alexa Developer Console
- [ngrok](https://ngrok.com/) con un túnel fijo
- Android Studio (para compilar GameController)

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
  "VolumenPasos": 3
}
```

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

El script `cmd/IniciarNGROK.cmd` automatiza esto (no incluido por contener credenciales).

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

La API queda disponible en `http://localhost:5780`. El túnel ngrok la expone públicamente.

En producción, una tarea del Programador de tareas de Windows (`task/Iniciar AlexaController.xml`) arranca la API automáticamente al iniciar sesión.

---

## GameController — Visor de log

La pantalla **Log** de la app Android muestra las entradas del log del servidor en tiempo real:

- **Auto-refresco** configurable (chip "Auto" en la barra superior)
- Refresco manual con el botón de actualizar
- **Color por nivel**: rojo (ERR), naranja (WRN), azul (INF), gris (DBG)
- Botón de **borrar** con confirmación — vacía el log en el servidor
- Scroll automático a la entrada más reciente

---

## Licencia

Distribuido bajo la licencia **GNU Lesser General Public License v2.1**.  
Consulta el archivo [LICENSE](LICENSE) para más detalles.
