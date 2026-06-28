// AlexaController - Sistema de automatización de PC por voz mediante Alexa.
// Copyright (C) 2026  José Luis Bautista Martín
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA

using System.Text.Json;

namespace AlexaController.Gestores
{
    public class ModoJuegosEstado
    {
        public bool Activo { get; set; }
        public bool Iniciando { get; set; }
        public DateTime? IniciadoEn { get; set; }
        public Dictionary<string, string> ProcesosDetenidos { get; set; } = new();
        public List<string> ServiciosDesactivados { get; set; } = new();
        public string? ModoMonitorAnterior { get; set; }
    }

    public class StateManager
    {
        private readonly string _rutaFichero;
        private readonly ILogger<StateManager> _logger;
        private ModoJuegosEstado _estado;
        private readonly object _lock = new();

        private static readonly JsonSerializerOptions _jsonOpts = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

        public StateManager(IConfiguration config, ILogger<StateManager> logger)
        {
            _rutaFichero = config["EstadoFilePath"] ?? "estado_modo_juegos.json";
            _logger = logger;
            _estado = CargarEstado();

            if (_estado.Iniciando)
            {
                _logger.LogWarning("La app arrancó con el modo juegos en estado 'iniciando'. Posible reinicio inesperado.");
                _estado.Iniciando = false;
                GuardarEstado();
            }
        }

        public ModoJuegosEstado ObtenerEstado()
        {
            lock (_lock) return _estado;
        }

        public void SetIniciando()
        {
            lock (_lock)
            {
                _estado.Iniciando = true;
                _estado.Activo = false;
                GuardarEstado();
            }
        }

        public void SetActivo(Dictionary<string, string> procesos, List<string> servicios, string modoMonitorAnterior)
        {
            lock (_lock)
            {
                _estado.Iniciando = false;
                _estado.Activo = true;
                _estado.IniciadoEn = DateTime.Now;
                _estado.ProcesosDetenidos = procesos;
                _estado.ServiciosDesactivados = servicios;
                _estado.ModoMonitorAnterior = modoMonitorAnterior;
                GuardarEstado();
            }
            _logger.LogInformation("Modo juegos activo. Procesos detenidos: {P}, servicios desactivados: {S}",
                procesos.Count, servicios.Count);
        }

        public void SetDetenido()
        {
            lock (_lock)
            {
                _estado = new ModoJuegosEstado();
                GuardarEstado();
            }
            _logger.LogInformation("Modo juegos detenido. Estado reseteado.");
        }

        private ModoJuegosEstado CargarEstado()
        {
            try
            {
                if (File.Exists(_rutaFichero))
                {
                    var json = File.ReadAllText(_rutaFichero);
                    return JsonSerializer.Deserialize<ModoJuegosEstado>(json, _jsonOpts) ?? new();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar el estado del modo juegos desde '{Path}'.", _rutaFichero);
            }
            return new ModoJuegosEstado();
        }

        private void GuardarEstado()
        {
            try
            {
                var json = JsonSerializer.Serialize(_estado, _jsonOpts);
                File.WriteAllText(_rutaFichero, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar el estado del modo juegos en '{Path}'.", _rutaFichero);
            }
        }
    }
}