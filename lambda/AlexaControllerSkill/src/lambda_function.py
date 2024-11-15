import json
import urllib.request

def lambda_handler(event, context):
    # Obtener el parámetro 'Action' de la solicitud de Alexa
    action = event['request']['intent']['slots'].get('Action', {}).get('value', '').lower()

    # Lista de acciones posibles
    valid_actions = [
        "apagarEquipo", "reiniciarEquipo", "iniciarSteam", "cerrarSteam", 
        "reiniciarSteam", "cerrarRetroArch", "iniciarModoJuegos", "detenerModoJuegos"
    ]
    
    # Verificar si la acción es válida
    if action not in valid_actions:
        message = "Acción no reconocida. Por favor, intente nuevamente."
        return {
            'version': '1.0',
            'response': {
                'outputSpeech': {
                    'type': 'PlainText',
                    'text': message
                },
                'shouldEndSession': True
            }
        }
    
    # URL base a la que se hará la solicitud
    url = f"https://absolute-jaybird-directly.ngrok-free.app/{action}"
    
    # Crear la solicitud con el encabezado personalizado
    request = urllib.request.Request(url)
    request.add_header("ngrok-skip-browser-warning", "false")
    
    try:
        # Realiza la solicitud a la página web
        response = urllib.request.urlopen(request)
        content = response.read().decode('utf-8')
        
        # Mensaje de respuesta según el contenido obtenido de la URL
        if "exito" in content.lower():
            message = f"La acción {action} se ha completado con éxito."
        else:
            message = f"No se pudo completar la acción {action}. Intente nuevamente."
        
    except Exception as e:
        # En caso de error en la solicitud
        message = "Hubo un problema al intentar realizar la acción."

    # Respuesta de Alexa
    return {
        'version': '1.0',
        'response': {
            'outputSpeech': {
                'type': 'PlainText',
                'text': message
            },
            'shouldEndSession': True
        }
    }
