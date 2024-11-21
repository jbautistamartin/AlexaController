import json
import urllib.request
import base64

def lambda_handler(event, context):

    # URLs de imágenes
    img_url_init = "https://cdn2.steamgriddb.com/thumb/1d0c4a0a5daed60c94b9b2106d1106d7.jpg"
    img_url_ok = "https://cdn2.steamgriddb.com/thumb/dc62a1f7be80fa5cbac62fdb8e997a5d.jpg"
    img_url_error = "https://cdn2.steamgriddb.com/thumb/6903dde95278048dc9c658c4b4493222.jpg"
    

    # Verificar si se recibe un intent en la solicitud
    if 'intent' not in event['request']:
        return build_apl_response(
            "Control de equipo listo",
            img_url_init,
            False
        )

    # Obtener el nombre del intent de la solicitud de Alexa
    intent_name = event['request']['intent']['name']
    
    # Diccionario con los posibles intents y las acciones asociadas
    intents = {
        "ApagarEquipoIntent": "apagarequipo",
        "ReiniciarEquipoIntent": "reiniciarequipo",
        "IniciarSteamIntent": "iniciarsteam",
        "CerrarSteamIntent": "cerrarsteam",
        "ReiniciarSteamIntent": "reiniciarsteam",
        "CerrarRetroarchIntent": "cerrarretroarch",
        "IniciarModoJuegosIntent": "iniciarmodojuegos",
        "DetenerModoJuegosIntent": "detenermodojuegos"
    }
    
 
    if intent_name not in intents:
        return build_apl_response("Intento no reconocido", img_url_init, False)
    
    action = intents[intent_name]
    url = f"https://absolute-jaybird-directly.ngrok-free.app/alexa/{action}"
    
    # Autenticación
    user = "admin"
    password = "password123"
    credentials = f"{user}:{password}"
    encoded_credentials = base64.b64encode(credentials.encode('utf-8')).decode('utf-8')
    
    # Solicitud
    request = urllib.request.Request(url)
    request.add_header("Authorization", f"Basic {encoded_credentials}")
    request.add_header("ngrok-skip-browser-warning", "false")
    
    try:
        response = urllib.request.urlopen(request)
        if response.status == 200:
            message = f"La acción '{action}' se ha completado con éxito."
            img_url = img_url_ok
            end_session = True
        else:
            message = f"Error al realizar la acción. Estado: {response.status}"
            img_url = img_url_error
            end_session = False
    except urllib.error.URLError as e:
        message = f"Hubo un problema: {e.reason}. Intente nuevamente."
        img_url = img_url_error
        end_session = False
    except Exception as e:
        message = f"Error desconocido: {str(e)}"
        img_url = img_url_error
        end_session = False

    return build_apl_response(message, img_url, end_session)

def build_apl_response(message, img_url, end_session):
    """
    Construye la respuesta de Alexa utilizando APL para mostrar contenido dinámico.
    """
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

    apl_data = {
        "data": {
            "message": message,
            "img_url": img_url
        }
    }

    return {
        'version': '1.0',
        'response': {
            'directives': [
                {
                    'type': 'Alexa.Presentation.APL.RenderDocument',
                    'token': 'aplToken',
                    'document': apl_document,
                    'datasources': apl_data
                }
            ],
            'shouldEndSession': end_session
        }
    }
