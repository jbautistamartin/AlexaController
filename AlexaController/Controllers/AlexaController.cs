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

using AlexaController.Gestores;
using AlexaController.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlexaController.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class AlexaController : ControllerBase
    {
        private readonly ILogger<AlexaController> _logger;
        private readonly EquipoHelper _equipoHelper;
        private readonly JuegosHelper _juegosHelper;
        private readonly ProcesosHelper _procesosHelper;
        private readonly SteamHelper _steamHelper;
        private readonly VolumeHelper _volumeHelper;
        private readonly JoypadHelper _joypadHelper;
        private readonly StateManager _stateManager;

        public AlexaController(
            ILogger<AlexaController> logger,
            EquipoHelper equipoHelper,
            JuegosHelper juegosHelper,
            ProcesosHelper procesosHelper,
            SteamHelper steamHelper,
            VolumeHelper volumeHelper,
            JoypadHelper joypadHelper,
            StateManager stateManager)
        {
            _logger = logger;
            _equipoHelper = equipoHelper;
            _juegosHelper = juegosHelper;
            _procesosHelper = procesosHelper;
            _steamHelper = steamHelper;
            _volumeHelper = volumeHelper;
            _joypadHelper = joypadHelper;
            _stateManager = stateManager;
        }

        [HttpGet(nameof(Estado))]
        public IActionResult Estado()
        {
            var estado = _stateManager.ObtenerEstado();
            return Ok(new
            {
                modoJuegos = estado.Activo ? "activo" : (estado.Iniciando ? "iniciando" : "inactivo"),
                iniciadoEn = estado.IniciadoEn,
                procesosDetenidos = estado.ProcesosDetenidos.Count,
                serviciosDesactivados = estado.ServiciosDesactivados.Count
            });
        }

        private string UsuarioActual => User.Identity?.Name ?? "desconocido";

        [HttpGet(nameof(ApagarEquipo))]
        public IActionResult ApagarEquipo()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Apagando equipo... (usuario: {Usuario})", usuario);
                    await _equipoHelper.ApagarEquipoAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al apagar el equipo.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(ReiniciarEquipo))]
        public IActionResult ReiniciarEquipo()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando equipo... (usuario: {Usuario})", usuario);
                    await _equipoHelper.ReiniciarEquipoAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar el equipo.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(IniciarSteam))]
        public IActionResult IniciarSteam()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Iniciando Steam... (usuario: {Usuario})", usuario);
                    await _steamHelper.IniciarSteamAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al iniciar Steam.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(CerrarSteam))]
        public IActionResult CerrarSteam()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Cerrando Steam... (usuario: {Usuario})", usuario);
                    await _steamHelper.CerrarSteamAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar Steam.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(ReiniciarSteam))]
        public IActionResult ReiniciarSteam()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Reiniciando Steam... (usuario: {Usuario})", usuario);
                    await _steamHelper.ReiniciarSteamAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reiniciar Steam.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(CerrarRetroArch))]
        public IActionResult CerrarRetroArch()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Cerrando RetroArch... (usuario: {Usuario})", usuario);
                    await _procesosHelper.CerrarRetroArchAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al cerrar RetroArch.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(IniciarModoJuegos))]
        public IActionResult IniciarModoJuegos()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Iniciando Modo Juegos... (usuario: {Usuario})", usuario);
                    await _juegosHelper.IniciarModoJuegosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al iniciar Modo Juegos.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(DetenerModoJuegos))]
        public IActionResult DetenerModoJuegos()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Deteniendo Modo Juegos... (usuario: {Usuario})", usuario);
                    await _juegosHelper.DetenerModoJuegosAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al detener el Modo Juegos.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(SubirVolumen))]
        public IActionResult SubirVolumen()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Subiendo volumen... (usuario: {Usuario})", usuario);
                    await _volumeHelper.SubirVolumenAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al subir el volumen.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(BajarVolumen))]
        public IActionResult BajarVolumen()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Bajando volumen... (usuario: {Usuario})", usuario);
                    await _volumeHelper.BajarVolumenAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al bajar el volumen.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(ReconectarMando))]
        public IActionResult ReconectarMando()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Reconectando mando... (usuario: {Usuario})", usuario);
                    await _joypadHelper.ReconectarMandoAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al reconectar el mando.");
                }
            });
            return Ok();
        }

        [HttpGet(nameof(Silenciar))]
        public IActionResult Silenciar()
        {
            var usuario = UsuarioActual;
            _ = Task.Run(async () =>
            {
                try
                {
                    _logger.LogInformation("Silenciando/activando volumen... (usuario: {Usuario})", usuario);
                    await _volumeHelper.SilenciarAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al silenciar el volumen.");
                }
            });
            return Ok();
        }
    }
}