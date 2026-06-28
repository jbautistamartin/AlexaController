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

using System.Runtime.InteropServices;

namespace AlexaController.Helpers
{
    public class VolumeHelper
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        private const byte VK_VOLUME_MUTE = 0xAD;
        private const byte VK_VOLUME_DOWN = 0xAE;
        private const byte VK_VOLUME_UP = 0xAF;
        private const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
        private const uint KEYEVENTF_KEYUP = 0x0002;

        private readonly ILogger<VolumeHelper> _logger;
        private readonly int _pasos;

        public VolumeHelper(ILogger<VolumeHelper> logger, IConfiguration config)
        {
            _logger = logger;
            _pasos = config.GetValue<int>("VolumenPasos", 3);
        }

        public Task SubirVolumenAsync()
        {
            for (int i = 0; i < _pasos; i++)
                PulsarTecla(VK_VOLUME_UP);
            _logger.LogInformation("Volumen subido ({Pasos} pasos).", _pasos);
            return Task.CompletedTask;
        }

        public Task BajarVolumenAsync()
        {
            for (int i = 0; i < _pasos; i++)
                PulsarTecla(VK_VOLUME_DOWN);
            _logger.LogInformation("Volumen bajado ({Pasos} pasos).", _pasos);
            return Task.CompletedTask;
        }

        public Task SilenciarAsync()
        {
            PulsarTecla(VK_VOLUME_MUTE);
            _logger.LogInformation("Volumen silenciado o reactivado.");
            return Task.CompletedTask;
        }

        private static void PulsarTecla(byte keyCode)
        {
            keybd_event(keyCode, 0, KEYEVENTF_EXTENDEDKEY, UIntPtr.Zero);
            keybd_event(keyCode, 0, KEYEVENTF_EXTENDEDKEY | KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}