Segunda versión publicada de **AlexaController**: control del PC por voz a través de un skill de
Alexa en español, o desde la aplicación Android **GameController**.

Lo nuevo de la 1.3.0 es que **las dos piezas avisan cuando hay una versión más nueva publicada**,
cada una por donde se la lee: el servicio del PC lo escribe en su log —que es el que se puede
mirar desde el móvil— y GameController lo marca en su pestaña «Acerca de», desde la que además se
descarga e instala el APK. Ninguna de las dos se actualiza sola.

El sistema sigue siendo cuatro piezas que se despliegan en cuatro sitios distintos, y **el orden
importa**: la Lambda necesita la URL del túnel, y el túnel necesita la API corriendo.

## Artefactos

| Fichero | Qué es | Dónde va |
|---|---|---|
| `AlexaController-1.3.0-win-x64.zip` | API .NET 10, autocontenida | el PC que se controla |
| `gamecontroller-1.3.0.apk` | Aplicación Android firmada | el móvil |
| `lambda-alexacontroller-1.3.0.zip` | Manejador del skill | AWS Lambda (Python 3.13) |
| `intents-1.3.0.json` | Modelo de interacción | Alexa Developer Console |

## Actualizar desde la 1.2.0

**El APK se instala encima** del que haya: va firmado con la misma clave, así que la
configuración —URL del túnel y credenciales— se conserva.

**La API hay que pararla antes de reemplazarla.** Si la arrancas con la tarea programada, detén la
tarea, sustituye la carpeta y vuelve a lanzarla. `appsettings.json` **no se sobrescribe solo**:
copia el tuyo o vuelve a rellenar `BasicAuth`, porque el del zip viene con los campos vacíos.

La Lambda y el modelo del skill **no han cambiado de contrato** en esta versión: si ya los tienes
subidos y funcionando, puedes dejarlos como están. Se publican igual para que el release lleve el
juego completo.

## Novedades

**Aviso de versión nueva en el servicio del PC.** Pregunta a GitHub 30 segundos después de
arrancar y luego cada 24 horas, y si hay un release más reciente lo escribe en el log con el
número y la dirección desde la que se descarga. Solo avisa: no se descarga ni se reemplaza a sí
mismo, porque hacerlo mientras tiene Steam en Big Picture y servicios de Windows detenidos por el
modo juegos es la forma de dejar el equipo a medias. Se apaga con
`"Actualizaciones": { "Comprobar": false }` en `appsettings.json`.

**Aviso de versión nueva en GameController.** Comprueba una vez al día, marca con un punto la
pestaña «Acerca de» y desde su tarjeta se puede ver qué versión hay, buscar a mano o
**descargar e instalar** el APK: lo baja el gestor de descargas del sistema y la instalación la
confirma Android, como con cualquier APK. El aviso se queda puesto entre aperturas hasta que se
instale la versión nueva, y se puede apagar con el interruptor de esa misma tarjeta.

**Enlace al proyecto en «Acerca de».** La tarjeta *Proyecto*, que ya nombraba la solución, lleva
ahora un botón que abre el repositorio en el navegador: apunta a la solución completa, no solo a
la aplicación de Android.

## Qué incluye (desde la 1.2.0)

**Órdenes de voz** — apagar y reiniciar el equipo, iniciar, cerrar y reiniciar Steam, cerrar
RetroArch, entrar y salir del modo juegos, enfocar y detener el juego en marcha, y subir, bajar o
silenciar el volumen.

**Modo juegos** — cierra las aplicaciones de fondo y sus ventanas, guarda la topología de
monitores y pasa a pantalla única, reconecta el mando, arranca Steam en Big Picture y detiene los
servicios de Windows que estorban. Al salir lo deshace todo. Una ventana de progreso acompaña el
proceso y se cierra cuando Steam ya se puede usar.

**Reparación del mando** — el receptor del Logitech F710 se queda con el problema PnP 28 («no hay
driver instalado») y Windows no vuelve a intentarlo por su cuenta. El modo juegos lo reinstala por
software.

**GameController** — las mismas órdenes desde el móvil, con visor del log del servidor en tiempo
real y coloreado por nivel, pestañas que se cambian deslizando y configuración editable en la
propia aplicación.

## Instalación desde cero

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

**4. Skill** — importar `intents-1.3.0.json` como modelo de interacción y apuntar el endpoint a la
Lambda. La invocación es «control de equipo».

**5. App Android** — instalar el APK y rellenar en **Ajustes** la URL del túnel y las credenciales.

Las mismas credenciales tienen que coincidir en tres sitios —`appsettings.json`, el `--basic-auth`
de ngrok y las variables de la Lambda—. Si no, la cadena se rompe sin dar un error claro.

## Notas

El APK y los binarios **no están firmados con un certificado de firma de código**, así que Windows
SmartScreen avisará la primera vez y Android pedirá permiso para instalar de orígenes desconocidos.
El APK sí va firmado con la clave de la casa, que es lo que permite actualizarlo encima del
anterior sin perder la configuración.
