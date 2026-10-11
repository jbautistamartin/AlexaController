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

using AlexaController.Helpers;
using Serilog.Core;
using Serilog.Events;

namespace AlexaController.UI
{
    /// <summary>
    /// Vuelca las trazas de la aplicación en la ventana de progreso mientras está abierta.
    /// Así el detalle que se ve en pantalla es el mismo del registro, sin tener que instrumentar
    /// cada helper con su propio informe de avance.
    /// </summary>
    internal sealed class ProgresoSink : ILogEventSink
    {
        private const string OrigenProgreso = "AlexaController.Helpers.ProgresoHelper";

        public void Emit(LogEvent logEvent)
        {
            var progreso = ProgresoHelper.Instancia;
            if (progreso is null || !progreso.Visible) return;

            // Los pasos ya se muestran como tales: no deben repetirse en el detalle.
            if (logEvent.Properties.TryGetValue(Constants.SourceContextPropertyName, out var origen) &&
                origen is ScalarValue { Value: string texto } && texto == OrigenProgreso)
                return;

            progreso.Detalle(logEvent.RenderMessage());
        }
    }
}
