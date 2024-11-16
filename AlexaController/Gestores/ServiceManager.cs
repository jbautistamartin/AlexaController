using System.ServiceProcess;

namespace AlexaController.Gestores
{
    public class ServiceManager
    {
        // Lista de servicios a gestionar con comentarios explicativos
        private readonly List<string> Services = new()
        {
            // Servicios relacionados con funciones específicas
            "Fax", // Servicio para enviar y recibir faxes
            "Spooler", // Maneja las colas de impresión (Print Spooler)
            "WSearch", // Servicio de indexación de búsqueda de Windows (Windows Search)
            "wuauserv", // Windows Update, controla las actualizaciones automáticas
            "DiagTrack", // Telemetría y diagnóstico, envía datos a Microsoft (Connected User Experiences and Telemetry)
            "seclogon", // Permite iniciar sesión con cuentas secundarias (Secondary Logon)
            "RemoteRegistry", // Permite editar el registro de manera remota
            "DPS", // Servicio de diagnóstico de problemas de red (Diagnostic Policy Service)
            "Offline Files", // Administra los archivos sin conexión, útil en redes corporativas
            "TrkWks", // Realiza seguimiento de enlaces entre archivos en red (Distributed Link Tracking Client)
            "TabletInputService", // Teclado táctil y panel de escritura a mano
            "bthserv", // Soporte para dispositivos Bluetooth (Bluetooth Support Service)
            "PcaSvc", // Ayuda con compatibilidad de programas antiguos (Program Compatibility Assistant Service)
            "SessionEnv", // Configuración de Escritorio Remoto (Remote Desktop Configuration)
            "TermService", // Servicios de Escritorio Remoto (Remote Desktop Services)
            "SCardSvr", // Soporte para tarjetas inteligentes (Smart Card)
            "ScDeviceEnum", // Enumeración de dispositivos de tarjetas inteligentes (Smart Card Device Enumeration Service)
            "WerSvc", // Reporte de errores a Microsoft (Windows Error Reporting Service)
            "WbioSrvc", // Autenticación biométrica (Windows Biometric Service)
            "FDResPub", // Publica dispositivos en red (Function Discovery Resource Publication)
            "RasAuto", // Conexión automática remota (Remote Access Auto Connection Manager)
            "RasMan", // Maneja conexiones VPN o remotas (Remote Access Connection Manager)
            "stisvc", // Acceso a cámaras y escáneres (Windows Image Acquisition)
            "wisvc", // Participación en el programa Windows Insider
            "WMPNetworkSvc", // Compartición de medios a través de red (Windows Media Player Network Sharing Service)
            "XboxGipSvc", // Servicios para juegos de Xbox (Xbox Live Auth Manager)
            "XblAuthManager", // Autenticación de Xbox Live
            "XblGameSave", // Guardado en la nube para juegos Xbox
            "XblNetSvc", // Red de Xbox Live
            "RetailDemo", // Modo demo de tiendas
            "MapsBroker", // Gestión de mapas descargados
            "ClickToRunSvc", // Actualización rápida de Microsoft Office (Office Click-to-Run)
            "Netlogon", // Solo útil en equipos unidos a un dominio
            "ShellHWDetection", // Detección de hardware para reproducción automática
            "CDPUserSvc", // Sincronización de dispositivos conectados (Connected Devices Platform Service)
            "AJRouter", // Comunicación entre dispositivos en red (AllJoyn Router Service)
            "AppReadiness", // Prepara apps al iniciar sesión
            "CloudPrint", // Servicio de impresión en la nube
            "WpnService", // Notificaciones push en Windows (Windows Push Notifications System Service)
            "NetTcpPortSharing", // Compartición de puertos TCP en red
            "HomeGroupProvider", // Para redes Grupo Hogar (antiguo)
            "HomeGroupListener", // Listener para Grupo Hogar
            "PhoneSvc", // Sincronización con teléfonos
            "icssvc", // Servicios de NFC y pagos (Payments and NFC/SE Manager)
            "MessagingService", // Notificaciones de SMS (Messaging Service)
            "lfsvc", // Servicio de geolocalización (Geolocation Service)
            "UmRdpService" // Redirección de dispositivos en Escritorio Remoto
        };

        // Almacena los servicios desactivados previamente
        private readonly HashSet<string> DisabledServices = new();

        private readonly ILogger<ServiceManager> _logger;

        public ServiceManager(ILogger<ServiceManager> logger)
        {
            _logger = logger;
        }

        // Método para desactivar los servicios
        public void DisableServices()
        {
            _logger.LogInformation("Desactivando servicios innecesarios...");
            foreach (var serviceName in Services)
            {
                try
                {
                    using var service = new ServiceController(serviceName);
                    if (service.Status == ServiceControllerStatus.Running)
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                        DisabledServices.Add(serviceName); // Registrar como desactivado
                        _logger.LogInformation($"Servicio '{serviceName}' desactivado.");
                    }
                    else
                    {
                        _logger.LogInformation($"Servicio '{serviceName}' ya estaba detenido.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al desactivar el servicio '{serviceName}': {ex.Message}");
                }
            }
        }

        // Método para reactivar los servicios previamente desactivados
        public void EnableServices()
        {
            Console.WriteLine("Reactivando servicios desactivados...");
            foreach (var serviceName in DisabledServices)
            {
                try
                {
                    using var service = new ServiceController(serviceName);
                    if (service.Status == ServiceControllerStatus.Stopped)
                    {
                        service.Start();
                        service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
                        _logger.LogInformation($"Servicio '{serviceName}' reactivado.");
                    }
                    else
                    {
                        _logger.LogInformation($"Servicio '{serviceName}' ya estaba en ejecución.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error al reactivar el servicio '{serviceName}': {ex.Message}");
                }
            }
        }
    }
}