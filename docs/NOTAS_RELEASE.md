Primer release publicado de **AlexaController**: control del PC por voz a través de un skill de
Alexa en español, o desde la aplicación Android **GameController**.

El sistema son cuatro piezas que se despliegan en cuatro sitios distintos, y **el orden importa**:
la Lambda necesita la URL del túnel, y el túnel necesita la API corriendo.

## Artefactos

| Fichero | Qué es | Dónde va |
|---|---|---|
| `AlexaController-1.2.0-win-x64.zip` | API .NET 10, autocontenida | el PC que se controla |
| `gamecontroller-1.2.0.apk` | Aplicación Android firmada | el móvil |
| `lambda-alexacontroller-1.2.0.zip` | Manejador del skill | AWS Lambda (Python 3.13) |
| `intents-1.2.0.json` | Modelo de interacción | Alexa Developer Console |

## Instalación

**1. API** — descomprimir el zip donde se quiera y editar `appsettings.json`: `BasicAuth.Username`
y `BasicAuth.Password` vienen **vacíos** a propósito y hay que rellenarlos. No hace falta instalar
.NET: el ejecutable lo lleva dentro.

> El ejecutable **pide elevación** (UAC). No es un descuido: sin privilegios de administrador no se
> puede reinstalar el receptor del mando ni detener servicios de Windows, que es la mitad del modo
> juegos. Para arrancarlo al iniciar sesión, la plantilla `task/Iniciar AlexaController.xml.example`
> del repositorio ya trae la tarea programada con privilegios máximos.

**2. Túnel** — `ngrok http 5780 --basic-auth "usuario:contraseña"`, con las mismas credenciales del
paso anterior. Plantilla en `cmd/IniciarNGROK.cmd.example`. Con un dominio reservado la URL deja de
cambiar en cada arranque, que es lo que permite dejarla fija en la Lambda.

**3. Lambda** — subir el zip a una función Python 3.13 con handler `lambda_function.lambda_handler`
y configurar tres variables de entorno: `API_URL` (la URL del túnel), `API_USER` y `API_PASSWORD`.
Sin dependencias que instalar: el manejador solo usa la biblioteca estándar.

**4. Skill** — importar `intents-1.2.0.json` como modelo de interacción y apuntar el endpoint a la
Lambda. La invocación es «control de equipo».

**5. App Android** — instalar el APK y rellenar en **Ajustes** la URL del túnel y las credenciales.

Las mismas credenciales tienen que coincidir en tres sitios —`appsettings.json`, el `--basic-auth`
de ngrok y las variables de la Lambda—. Si no, la cadena se rompe sin dar un error claro.

## Qué incluye

**Órdenes de voz** — apagar y reiniciar el equipo, iniciar/cerrar/reiniciar Steam, cerrar RetroArch,
entrar y salir del modo juegos, enfocar y detener el juego en marcha, y subir, bajar o silenciar el
volumen.

**Modo juegos** — cierra las aplicaciones de fondo y sus ventanas, guarda la topología de monitores
y pasa a pantalla única, reconecta el mando, arranca Steam en Big Picture y detiene los servicios de
Windows que estorban. Al salir lo deshace todo, incluidas las aplicaciones que había cerrado. Una
ventana de progreso acompaña el proceso y se cierra justo cuando Steam ya se puede usar.

**Reparación del mando** — el receptor del Logitech F710 se queda con el problema PnP 28 («no hay
driver instalado») y Windows no vuelve a intentarlo por su cuenta. El modo juegos lo reinstala por
software: limpia el `ConfigFlags` del nodo fallido, borra la caché de descriptores USB y, si hace
falta, cicla el concentrador para forzar una lectura completa.

**GameController** — las mismas órdenes desde el móvil, con visor del log del servidor en tiempo
real y coloreado por nivel. Las cuatro pestañas se cambian deslizando, y la configuración se puede
exportar e importar como JSON.

## Avisos

La aplicación se entrega **tal cual**. Apaga y reinicia el PC, cierra ventanas sin preguntar y
detiene servicios de Windows: quien la instala y la configura es el único responsable de su uso.

La API queda expuesta en Internet a través del túnel, protegida únicamente con autenticación
básica. Conviene una contraseña larga y no reutilizada.

Distribuido bajo **LGPL v2.1**.
