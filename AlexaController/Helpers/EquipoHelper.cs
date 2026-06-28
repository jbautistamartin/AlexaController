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

using System.Diagnostics;

namespace AlexaController.Helpers
{
    public class EquipoHelper
    {
        private readonly ILogger<EquipoHelper> _logger;

        public EquipoHelper(ILogger<EquipoHelper> logger)
        {
            _logger = logger;
        }

        public async Task ApagarEquipoAsync()
        {
            await Task.Run(() =>
            {
                Process.Start("shutdown", "/s /t 0");
                _logger.LogInformation("Comando de apagado enviado al sistema.");
            });
        }

        public async Task ReiniciarEquipoAsync()
        {
            await Task.Run(() =>
            {
                Process.Start("shutdown", "/r /t 0");
                _logger.LogInformation("Comando de reinicio enviado al sistema.");
            });
        }
    }
}