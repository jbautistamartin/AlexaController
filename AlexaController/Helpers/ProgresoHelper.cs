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

using System.Runtime.Versioning;
using AlexaController.UI;

namespace AlexaController.Helpers
{
    /// <summary>
    /// Muestra en pantalla el avance de una operación larga (el modo juegos tarda cerca de
    /// un minuto y sin esto parece que no ocurre nada). La ventana vive en su propio hilo
    /// STA con su bucle de mensajes, de modo que sigue repintándose aunque el trabajo esté
    /// bloqueando el hilo que la maneja.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class ProgresoHelper : IDisposable
    {
        private readonly ILogger<ProgresoHelper> _logger;
        private readonly bool _habilitado;
        private readonly object _sync = new();

        private Thread? _hilo;
        private VentanaProgreso? _ventana;

        /// <summary>
        /// Instancia activa, para que el sumidero de Serilog pueda volcar las trazas en la
        /// ventana sin obligar a cada helper a informar de su propio progreso.
        /// </summary>
        public static ProgresoHelper? Instancia { get; private set; }

        public ProgresoHelper(ILogger<ProgresoHelper> logger, IConfiguration config)
        {
            _logger = logger;
            _habilitado = config.GetValue("MostrarProgreso", true);
            Instancia = this;
        }

        /// <summary>Hay una ventana de progreso en pantalla.</summary>
        public bool Visible => _ventana is { IsDisposed: false };

        /// <summary>Abre la ventana. Si ya había una, la sustituye.</summary>
        public void Iniciar(string titulo, int totalPasos)
        {
            if (!_habilitado) return;

            lock (_sync)
            {
                CerrarInterno();

                var listo = new ManualResetEventSlim(false);

                _hilo = new Thread(() =>
                {
                    try
                    {
                        Application.EnableVisualStyles();
                        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);

                        var ventana = new VentanaProgreso(titulo, totalPasos);
                        ventana.Shown += (_, _) => listo.Set();
                        _ventana = ventana;

                        Application.Run(ventana);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "No se pudo mostrar la ventana de progreso.");
                    }
                    finally
                    {
                        _ventana = null;
                        listo.Set();
                    }
                })
                {
                    IsBackground = true,
                    Name = "VentanaProgreso"
                };

                _hilo.SetApartmentState(ApartmentState.STA);
                _hilo.Start();

                // Sin esperar al primer pintado, los primeros pasos se perderían.
                listo.Wait(TimeSpan.FromSeconds(5));
            }
        }

        /// <summary>Avanza al siguiente paso y lo describe.</summary>
        public void Paso(string texto)
        {
            _logger.LogInformation("[progreso] {Texto}", texto);
            EnVentana(v => v.EstablecerPaso(texto));
        }

        /// <summary>Añade una línea de detalle bajo el paso actual (últimas trazas).</summary>
        public void Detalle(string texto) => EnVentana(v => v.AnadirDetalle(texto));

        /// <summary>Marca el final, deja el mensaje visible un instante y cierra la ventana.</summary>
        public async Task CompletarAsync(string texto, int msVisible = 1500)
        {
            if (!Visible) return;

            EnVentana(v => v.Completar(texto));
            await Task.Delay(msVisible);
            Cerrar();
        }

        /// <summary>Cierra la ventana si sigue abierta. Es idempotente.</summary>
        public void Cerrar()
        {
            lock (_sync) CerrarInterno();
        }

        public void Dispose() => Cerrar();

        private void CerrarInterno()
        {
            var ventana = _ventana;
            _ventana = null;
            if (ventana is null) return;

            try
            {
                if (ventana.IsHandleCreated && !ventana.IsDisposed)
                    ventana.BeginInvoke(ventana.Close);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al cerrar la ventana de progreso.");
            }

            // Application.Run termina al cerrarse la ventana; si no lo hace, el hilo es de
            // segundo plano y no impedirá que la aplicación se cierre.
            _hilo?.Join(TimeSpan.FromSeconds(2));
            _hilo = null;
        }

        private void EnVentana(Action<VentanaProgreso> accion)
        {
            var ventana = _ventana;
            if (ventana is null || ventana.IsDisposed || !ventana.IsHandleCreated) return;

            try
            {
                ventana.BeginInvoke(accion, ventana);
            }
            catch (Exception)
            {
                // La ventana se está cerrando: el progreso nunca debe romper la operación.
            }
        }
    }
}
