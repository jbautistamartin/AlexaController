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

using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AlexaController.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class LogController : ControllerBase
    {
        private readonly ILogger<LogController> _logger;
        private readonly IWebHostEnvironment _env;

        private static readonly Regex LogLineRegex =
            new(@"^\[(\d{2}:\d{2}:\d{2}) ([A-Z]{3})\] (.+)$", RegexOptions.Compiled);

        public LogController(ILogger<LogController> logger, IWebHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        [HttpGet("Obtener")]
        public IActionResult ObtenerLog()
        {
            var ruta = ObtenerRutaLogActual();
            if (!System.IO.File.Exists(ruta))
                return Ok(new { entradas = Array.Empty<object>() });

            try
            {
                using var stream = new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var lineas = reader.ReadToEnd()
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Select(l => l.TrimEnd('\r'))
                    .ToArray();

                var entradas = ParsearLog(lineas)
                    .Where(e => !e.Mensaje.Contains("[SECURITY]", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                return Ok(new { entradas });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer el log.");
                return StatusCode(500, "Error al leer el log.");
            }
        }

        [HttpDelete("Borrar")]
        public IActionResult BorrarLog()
        {
            var ruta = ObtenerRutaLogActual();
            if (!System.IO.File.Exists(ruta))
                return Ok();

            try
            {
                using var stream = new FileStream(ruta, FileMode.Open, FileAccess.Write, FileShare.ReadWrite);
                stream.SetLength(0);
                _logger.LogInformation("Log borrado por {Usuario}.", User.Identity?.Name ?? "desconocido");
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al borrar el log.");
                return StatusCode(500, "Error al borrar el log.");
            }
        }

        private static string ObtenerRutaLogActual()
        {
            var asm = Assembly.GetExecutingAssembly();
            return Path.Combine(
                AppContext.BaseDirectory,
                asm.GetName().Name + ".log");
        }

        private static List<LogEntradaDto> ParsearLog(string[] lineas)
        {
            var entradas = new List<LogEntradaDto>();
            LogEntradaDto? actual = null;
            var excepcion = new StringBuilder();

            foreach (var linea in lineas)
            {
                var match = LogLineRegex.Match(linea);
                if (match.Success)
                {
                    if (actual != null)
                    {
                        entradas.Add(actual with { Excepcion = excepcion.Length > 0 ? excepcion.ToString().TrimEnd() : null });
                        excepcion.Clear();
                    }
                    actual = new LogEntradaDto(match.Groups[1].Value, match.Groups[2].Value, match.Groups[3].Value);
                }
                else if (actual != null && !string.IsNullOrWhiteSpace(linea))
                {
                    if (excepcion.Length > 0) excepcion.AppendLine();
                    excepcion.Append(linea);
                }
            }

            if (actual != null)
                entradas.Add(actual with { Excepcion = excepcion.Length > 0 ? excepcion.ToString().TrimEnd() : null });

            return entradas;
        }
    }

    public record LogEntradaDto(string Hora, string Nivel, string Mensaje, string? Excepcion = null);
}
