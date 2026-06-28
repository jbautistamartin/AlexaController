# AlexaController - Sistema de automatización de PC por voz mediante Alexa.
# Copyright (C) 2026  José Luis Bautista Martín
#
# This library is free software; you can redistribute it and/or
# modify it under the terms of the GNU Lesser General Public
# License as published by the Free Software Foundation; either
# version 2.1 of the License, or (at your option) any later version.
#
# This library is distributed in the hope that it will be useful,
# but WITHOUT ANY WARRANTY; without even the implied warranty of
# MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
# Lesser General Public License for more details.
#
# You should have received a copy of the GNU Lesser General Public
# License along with this library; if not, write to the Free Software
# Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

import json
import urllib.request
import urllib.error
import base64
import os

# TODO: Configurar las siguientes variables de entorno en la consola de AWS Lambda
#       (Configuración > Variables de entorno):
#   - API_URL: URL pública del túnel ngrok (ej: https://xxxx.ngrok-free.app)
#   - API_USER: Usuario configurado en BasicAuth de la API y en ngrok --basic-auth
#   - API_PASSWORD: Contraseña configurada en BasicAuth de la API y en ngrok --basic-auth
API_URL = os.environ.get("API_URL", "") + "/alexa/{action}"
API_USER = os.environ.get("API_USER", "")
API_PASSWORD = os.environ.get("API_PASSWORD", "")

IMG_INIT  = "https://cdn2.steamgriddb.com/thumb/1d0c4a0a5daed60c94b9b2106d1106d7.jpg"
IMG_OK    = "https://cdn2.steamgriddb.com/thumb/dc62a1f7be80fa5cbac62fdb8e997a5d.jpg"
IMG_ERROR = "https://cdn2.steamgriddb.com/thumb/6903dde95278048dc9c658c4b4493222.jpg"

INTENTS = {
    "ApagarEquipoIntent":       "apagarequipo",
    "ReiniciarEquipoIntent":    "reiniciarequipo",
    "IniciarSteamIntent":       "iniciarsteam",
    "CerrarSteamIntent":        "cerrarsteam",
    "ReiniciarSteamIntent":     "reiniciarsteam",
    "CerrarRetroarchIntent":    "cerrarretroarch",
    "IniciarModoJuegosIntent":  "iniciarmodojuegos",
    "DetenerModoJuegosIntent":  "detenermodojuegos",
    "EstadoIntent":             "estado",
    "SubirVolumenIntent":       "subirvolumen",
    "BajarVolumenIntent":       "bajarvolumen",
    "SilenciarIntent":          "silenciar",
}

MENSAJES_OK = {
    "apagarequipo":       "Apagando el equipo. Hasta pronto.",
    "reiniciarequipo":    "Reiniciando el equipo.",
    "iniciarsteam":       "Steam en marcha. A jugar.",
    "cerrarsteam":        "Steam cerrado.",
    "reiniciarsteam":     "Reiniciando Steam.",
    "cerrarretroarch":    "Emulador cerrado.",
    "iniciarmodojuegos":  "Modo juegos activando.",
    "detenermodojuegos":  "Modo juegos desactivado.",
    "subirvolumen":       "Volumen subido.",
    "bajarvolumen":       "Volumen bajado.",
    "silenciar":          "Silenciado.",
}

# Acciones que requieren confirmación previa
ACCIONES_CON_CONFIRMACION = {"apagarequipo"}


def lambda_handler(event, context):
    supports_apl = (
        "supportedInterfaces" in event["context"]["System"]["device"]
        and "Alexa.Presentation.APL" in event["context"]["System"]["device"]["supportedInterfaces"]
    )

    session_attrs = event.get("session", {}).get("attributes", {})

    if "intent" not in event["request"]:
        return build_response("Control de equipo listo.", IMG_INIT, False, supports_apl, session_attrs)

    intent_name = event["request"]["intent"]["name"]

    # --- Confirmación pendiente ---
    if intent_name == "AMAZON.YesIntent":
        accion_pendiente = session_attrs.pop("accion_pendiente", None)
        if not accion_pendiente:
            return build_response("No hay ninguna acción pendiente.", IMG_ERROR, True, supports_apl, {})
        return ejecutar_accion(accion_pendiente, supports_apl, session_attrs)

    if intent_name == "AMAZON.NoIntent":
        session_attrs.pop("accion_pendiente", None)
        return build_response("Acción cancelada.", IMG_INIT, True, supports_apl, {})

    if intent_name in ("AMAZON.CancelIntent", "AMAZON.StopIntent"):
        return build_response("Hasta pronto.", IMG_INIT, True, supports_apl, {})

    if intent_name == "AMAZON.HelpIntent":
        ayuda = ("Puedo apagar o reiniciar el equipo, controlar Steam y RetroArch, "
                 "activar el modo juegos, ajustar el volumen y consultar el estado. ¿Qué quieres hacer?")
        return build_response(ayuda, IMG_INIT, False, supports_apl, session_attrs)

    if intent_name not in INTENTS:
        return build_response("Comando no reconocido.", IMG_ERROR, True, supports_apl, {})

    accion = INTENTS[intent_name]

    # --- Pedir confirmación si la acción lo requiere ---
    if accion in ACCIONES_CON_CONFIRMACION:
        session_attrs["accion_pendiente"] = accion
        pregunta = "¿Seguro que quieres apagar el equipo?"
        return build_response(pregunta, IMG_INIT, False, supports_apl, session_attrs)

    return ejecutar_accion(accion, supports_apl, session_attrs)


def ejecutar_accion(accion, supports_apl, session_attrs):
    credentials = base64.b64encode(f"{API_USER}:{API_PASSWORD}".encode()).decode()
    url = API_URL.format(action=accion)

    try:
        req = urllib.request.Request(url)
        req.add_header("Authorization", f"Basic {credentials}")
        req.add_header("ngrok-skip-browser-warning", "true")
        response = urllib.request.urlopen(req, timeout=5)

        if accion == "estado":
            return manejar_estado(response, supports_apl)

        mensaje = MENSAJES_OK.get(accion, f"Acción '{accion}' completada.")
        return build_response(mensaje, IMG_OK, True, supports_apl, {})

    except urllib.error.URLError as e:
        return build_response(f"No pude conectar con el equipo: {e.reason}", IMG_ERROR, False, supports_apl, {})
    except Exception as e:
        return build_response(f"Error inesperado: {str(e)}", IMG_ERROR, False, supports_apl, {})


def manejar_estado(response, supports_apl):
    try:
        datos = json.loads(response.read().decode())
        modo = datos.get("modoJuegos", "inactivo")
        if modo == "activo":
            procs = datos.get("procesosDetenidos", 0)
            servs = datos.get("serviciosDesactivados", 0)
            mensaje = f"El modo juegos está activo. He detenido {procs} programas y {servs} servicios."
            img = IMG_OK
        elif modo == "iniciando":
            mensaje = "El modo juegos se está activando todavía. Espera un momento y vuelve a preguntarme."
            img = IMG_INIT
        else:
            mensaje = "El modo juegos está inactivo."
            img = IMG_INIT
    except Exception:
        mensaje = "No pude leer el estado del equipo."
        img = IMG_ERROR

    return build_response(mensaje, img, True, supports_apl, {})


def build_response(message, img_url, end_session, supports_apl, session_attrs):
    if supports_apl:
        return build_apl_response(message, img_url, end_session, session_attrs)
    return build_text_response(message, end_session, session_attrs)


def build_apl_response(message, img_url, end_session, session_attrs):
    apl_document = {
        "type": "APL",
        "version": "1.8",
        "mainTemplate": {
            "parameters": ["payload"],
            "items": [
                {
                    "type": "Container",
                    "height": "100%",
                    "width": "100%",
                    "items": [
                        {
                            "type": "Image",
                            "source": "${payload.data.img_url}",
                            "width": "100%",
                            "height": "100%",
                            "scale": "best-fill"
                        },
                        {
                            "type": "Text",
                            "text": "${payload.data.message}",
                            "style": "textStyleBody"
                        }
                    ]
                }
            ]
        }
    }

    return {
        "version": "1.0",
        "sessionAttributes": session_attrs,
        "response": {
            "directives": [
                {
                    "type": "Alexa.Presentation.APL.RenderDocument",
                    "token": "aplToken",
                    "document": apl_document,
                    "datasources": {"data": {"message": message, "img_url": img_url}}
                }
            ],
            "outputSpeech": {"type": "PlainText", "text": message},
            "shouldEndSession": end_session
        }
    }


def build_text_response(message, end_session, session_attrs):
    return {
        "version": "1.0",
        "sessionAttributes": session_attrs,
        "response": {
            "outputSpeech": {"type": "PlainText", "text": message},
            "shouldEndSession": end_session
        }
    }
