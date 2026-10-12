# Registro de cambios

Formato basado en [Keep a Changelog](https://keepachangelog.com/es-ES/1.1.0/);
versionado según [SemVer](https://semver.org/lang/es/).

Las notas largas de cada release —artefactos, instalación paso a paso y en qué orden— viven en
[`docs/NOTAS_RELEASE.md`](docs/NOTAS_RELEASE.md), que es el fichero que `release.ps1` publica en
GitHub. Este registro es el resumen de qué cambió.

## [1.3.0] - 2026-10-11

### Añadido

- **Aviso de versión nueva, en las dos piezas.** El servicio del PC pregunta a GitHub una vez al
  día y lo escribe en su log, que es el que se puede leer desde el móvil; y **GameController** lo
  comprueba también, marca con un punto la pestaña «Acerca de» y deja descargar e instalar el APK
  desde ahí. Los dos solo avisan de lo que hay publicado: el servicio no se reemplaza a sí mismo, y
  la instalación del APK la confirma Android. Se apaga con `"Actualizaciones": { "Comprobar": false }`
  en `appsettings.json` y con el interruptor de la tarjeta en el mando.

- **GameController: enlace al proyecto en «Acerca de».** La tarjeta *Proyecto*, que ya nombraba
  la solución, lleva ahora un botón que abre la página del repositorio en el navegador. Apunta a
  la solución completa, no solo a la aplicación de Android: el servicio del PC, la Lambda y el
  modelo del skill se publican ahí mismo.

## [1.2.0] - 2026-10-11

Primer release publicado. Las versiones anteriores no salieron del repositorio, así que este
registro empieza aquí.

### Añadido

- **Órdenes de voz** a través del skill de Alexa en español: apagar y reiniciar el equipo,
  iniciar, cerrar y reiniciar Steam, cerrar RetroArch, entrar y salir del modo juegos, enfocar y
  detener el juego en marcha, y subir, bajar o silenciar el volumen.
- **Modo juegos.** Cierra las aplicaciones de fondo y sus ventanas, guarda la topología de
  monitores y pasa a pantalla única, reconecta el mando, arranca Steam en Big Picture y detiene
  los servicios de Windows que estorban. Al salir lo deshace todo, incluidas las aplicaciones que
  había cerrado. Una ventana de progreso acompaña el proceso y se cierra cuando Steam ya se puede
  usar.
- **Reparación del mando.** El receptor del Logitech F710 se queda con el problema PnP 28 («no
  hay driver instalado») y Windows no vuelve a intentarlo por su cuenta. El modo juegos lo
  reinstala por software: limpia el `ConfigFlags` del nodo fallido, borra la caché de
  descriptores USB y, si hace falta, cicla el concentrador para forzar una lectura completa.
- **GameController**, la aplicación Android: las mismas órdenes desde el móvil, con visor del log
  del servidor en tiempo real y coloreado por nivel, pestañas que se cambian deslizando y
  configuración editable en la propia aplicación.
- **Cuatro artefactos publicados** en el release: la API autocontenida para el PC (no hace falta
  instalar .NET), el APK firmado de GameController, el zip de la función Lambda y el modelo de
  interacción del skill.

### Notas

- La API **pide elevación** (UAC) a propósito: sin privilegios de administrador no se puede
  reinstalar el receptor del mando ni detener servicios de Windows, que es la mitad del modo
  juegos.
- Las credenciales de autenticación básica tienen que coincidir en tres sitios —
  `appsettings.json`, el `--basic-auth` de ngrok y las variables de la Lambda—. Si no, la cadena
  se rompe sin dar un error claro.
