import json
import urllib.request
import base64

def lambda_handler(event, context):
    # Verificar si se recibe un intent en la solicitud
    if 'intent' not in event['request']:
        # Si no se recibe intent, devolver mensaje "Control de equipo listo"
        return build_response("Control de equipo listo", False)

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
    
    # Si el intent no es válido, devolver mensaje "Control de equipo listo"
    if intent_name not in intents:
        return build_response("Control de equipo listo", False)
    
    # Obtener la acción correspondiente al intent
    action = intents[intent_name]
    
    # URL base a la que se hará la solicitud
    url = f"https://absolute-jaybird-directly.ngrok-free.app/alexa/{action}"
    
    # Crear el encabezado de autenticación básica
    user = "admin"
    password = "password123"
    credentials = f"{user}:{password}"
    encoded_credentials = base64.b64encode(credentials.encode('utf-8')).decode('utf-8')
    
    # Crear la solicitud con el encabezado personalizado para la autenticación básica
    request = urllib.request.Request(url)
    request.add_header("Authorization", f"Basic {encoded_credentials}")
    request.add_header("ngrok-skip-browser-warning", "false")
    
    try:
        # Realiza la solicitud a la página web
        response = urllib.request.urlopen(request)
        content = response.read().decode('utf-8')
        
        # Verifica si la respuesta fue exitosa
        if response.status == 200:
            message = f"La acción '{action}' se ha completado con éxito."
        else:
            message = f"Hubo un error al realizar la acción. Estado: {response.status}"
      
    except urllib.error.URLError as e:
        # En caso de error de red o URL
        message = f"Hubo un problema al intentar realizar la acción: {e.reason}. Intente nuevamente."
        print(f"Error de conexión: {e}")
    except Exception as e:
        # En caso de error inesperado
        message = f"Hubo un problema al intentar realizar la acción. {str(e)}"
        print(f"Error desconocido: {str(e)}")

    # Respuesta de Alexa
    return build_response(message, False)

def build_response(message, end_session):
    """
    Función auxiliar para construir la respuesta de Alexa.
    """
    return {
        'version': '1.0',
        'response': {
            'outputSpeech': {
                'type': 'PlainText',
                'text': message
            },
            'shouldEndSession': end_session
        }
    }
