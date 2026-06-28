# AlexaController

Sistema de automatización de PC mediante comandos de voz a través de un skill de Alexa en español.

## ¿Qué hace?

Permite controlar un PC de forma remota usando la voz. Al decir "Alexa, dile a control de equipo que apague el equipo", Alexa envía el comando a una API local que ejecuta la acción correspondiente.

Acciones disponibles:
- Apagar y reiniciar el equipo
- Iniciar, cerrar y reiniciar Steam
- Cerrar RetroArch
- Activar y desactivar el modo juegos (cambia el monitor, detiene procesos y servicios en segundo plano)
- Subir, bajar y silenciar el volumen
- Consultar el estado del modo juegos

## Arquitectura

```
Voz → Alexa Cloud → AWS Lambda → API ASP.NET Core (PC local)
```

1. El usuario da un comando de voz a Alexa.
2. Alexa Cloud lo procesa y llama a la función **AWS Lambda** (`lambda/AlexaController/src/lambda_function.py`).
3. Lambda traduce el intent al endpoint correspondiente y llama a `GET /alexa/{accion}` con autenticación básica.
4. La **API ASP.NET Core** (`.NET 10`, ejecutándose en el PC) recibe la petición, responde inmediatamente con HTTP 200 y ejecuta la acción en segundo plano.
5. El PC ejecuta la acción (apagado, procesos, volumen, etc.).

La API local se expone a internet mediante un túnel **ngrok**.

## Requisitos

- Windows 10/11
- .NET 10 SDK
- Cuenta de AWS con una función Lambda configurada
- Skill de Alexa publicado en Alexa Developer Console
- [ngrok](https://ngrok.com/) con un túnel fijo

## Configuración

### 1. API local (`AlexaController/appsettings.json`)

Copia el archivo de ejemplo y rellena los valores:

```json
{
  "BasicAuth": {
    "Username": "tu_usuario",
    "Password": "tu_contraseña"
  },
  "SteamPath": "C:\\Program Files (x86)\\Steam\\"
}
```

Las credenciales deben coincidir con las configuradas en ngrok (`--basic-auth "usuario:contraseña"`).

### 2. Función Lambda (variables de entorno)

En la consola de AWS Lambda, configurar en **Configuración > Variables de entorno**:

| Variable | Descripción |
|---|---|
| `API_URL` | URL pública del túnel ngrok (ej: `https://xxxx.ngrok-free.app`) |
| `API_USER` | Usuario de autenticación básica |
| `API_PASSWORD` | Contraseña de autenticación básica |

### 3. ngrok

Iniciar el túnel con autenticación básica:

```cmd
ngrok http 5780 --basic-auth "usuario:contraseña"
```

Para automatizar el arranque se incluye el script `cmd/IniciarNGROK.cmd` (no incluido en el repositorio por contener credenciales).

## Ejecución

```bash
# Desarrollo
dotnet run --project AlexaController

# Producción
dotnet build --configuration Release
```

La API queda disponible en `http://localhost:5780`. El túnel ngrok la expone públicamente.

En producción, una tarea del Programador de tareas de Windows (`task/Iniciar AlexaController.xml`) arranca la API automáticamente al iniciar sesión.

## Estructura del proyecto

```
AlexaController/          API ASP.NET Core
  Controllers/            Endpoints /alexa/{accion}
  Helpers/                Lógica de cada acción (Steam, volumen, monitor...)
  Gestores/               Estado del modo juegos (procesos, servicios, monitor)
  Seguridad/              Autenticación básica
AlexaControllerSkill/     Modelo de interacción del skill de Alexa
lambda/                   Función AWS Lambda (Python 3.13)
```

## Licencia

Este proyecto se distribuye bajo la licencia **GNU Lesser General Public License v2.1**. Consulta el archivo [LICENSE](LICENSE) para más detalles.
