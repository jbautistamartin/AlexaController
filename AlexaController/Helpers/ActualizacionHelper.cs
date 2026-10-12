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

using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;

namespace AlexaController.Helpers;

/// <summary>
/// Mira si en GitHub hay publicada una versión más nueva de AlexaController y lo deja dicho en
/// el log.
///
/// **Solo avisa.** Aquí no se descarga ni se instala nada, y no es por falta de ganas: esto es
/// un servicio que el PC levanta al iniciar sesión, que pide elevación y que mientras corre
/// puede tener Steam abierto en Big Picture y servicios de Windows detenidos por el modo juegos.
/// Reemplazarse a sí mismo en ese estado es la clase de automatismo que deja el equipo a medias;
/// el release se instala a mano, con la aplicación parada, que es también lo que dicen sus notas.
///
/// El aviso sale **por el log**, que es justo donde se va a leer: GameController trae un visor
/// del log del servidor en tiempo real, así que una línea aquí se ve desde el móvil sin montar
/// nada más. Si no hay red, no se dice nada: no haber podido comprobar no es una noticia.
/// </summary>
public sealed class ActualizacionHelper : BackgroundService
{
    private const string UrlApi =
        "https://api.github.com/repos/jbautistamartin/AlexaController/releases/latest";

    /// <summary>Lo que se espera antes de la primera comprobación, para no estorbar al arranque.</summary>
    private static readonly TimeSpan EsperaInicial = TimeSpan.FromSeconds(30);

    /// <summary>Cada cuánto se vuelve a preguntar mientras el servicio siga vivo.</summary>
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(24);

    private readonly ILogger<ActualizacionHelper> _log;
    private readonly bool _comprobar;

    public ActualizacionHelper(ILogger<ActualizacionHelper> log, IConfiguration configuracion)
    {
        _log = log;
        _comprobar = configuracion.GetValue("Actualizaciones:Comprobar", true);
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!_comprobar)
        {
            _log.LogInformation("Comprobación de versiones desactivada por configuración.");
            return;
        }

        try
        {
            await Task.Delay(EsperaInicial, ct);

            while (!ct.IsCancellationRequested)
            {
                await ComprobarAsync(ct);
                await Task.Delay(Intervalo, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // La aplicación se está cerrando: no hay nada que decir.
        }
    }

    private async Task ComprobarAsync(CancellationToken ct)
    {
        try
        {
            using var cliente = new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
            // Sin User-Agent, api.github.com responde 403. No es opcional.
            cliente.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("AlexaController", VersionInstalada()?.ToString() ?? "1.0"));
            cliente.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var respuesta = await cliente.GetAsync(UrlApi, ct);

            // 404 significa que todavía no hay releases publicados, no que algo vaya mal.
            if (!respuesta.IsSuccessStatusCode)
            {
                _log.LogDebug(
                    "No se pudo comprobar si hay versión nueva: HTTP {Codigo}",
                    (int)respuesta.StatusCode);
                return;
            }

            using var flujo = await respuesta.Content.ReadAsStreamAsync(ct);
            using var json = await JsonDocument.ParseAsync(flujo, cancellationToken: ct);

            string etiqueta = json.RootElement.TryGetProperty("tag_name", out var tag)
                ? tag.GetString() ?? string.Empty
                : string.Empty;

            var publicada = Interpretar(etiqueta);
            var instalada = VersionInstalada();

            if (publicada is null || instalada is null)
            {
                return;
            }

            if (publicada <= instalada)
            {
                _log.LogInformation(
                    "AlexaController está al día: la versión {Instalada} es la última publicada.",
                    instalada);
                return;
            }

            string pagina = json.RootElement.TryGetProperty("html_url", out var url)
                ? url.GetString() ?? string.Empty
                : string.Empty;

            // Se dice en una línea y con lo que hace falta para actuar: el número y dónde está.
            // El release publica las cuatro piezas juntas, así que si hay versión nueva también
            // la hay del mando, de la Lambda y del modelo del skill.
            _log.LogWarning(
                "Hay una versión nueva de AlexaController: {Publicada} (instalada: {Instalada}). " +
                "Se descarga de {Pagina} y se instala con la aplicación parada; el release lleva " +
                "también el APK del mando, la Lambda y el modelo del skill.",
                publicada, instalada, pagina);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Sin red o con el DNS caído. Se queda en debug: no es un problema del equipo.
            _log.LogDebug(ex, "No se pudo comprobar si hay versión nueva.");
        }
    }

    /// <summary>La versión del ensamblado, que es la que fija el <c>&lt;Version&gt;</c> del csproj.</summary>
    private static Version? VersionInstalada()
    {
        var ensamblado = Assembly.GetExecutingAssembly();
        string? informativa = ensamblado
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        return Interpretar(informativa) ?? ensamblado.GetName().Version;
    }

    /// <summary>
    /// Convierte «v1.2.0», «1.2.0» o «1.2.0+sha» en un <see cref="Version"/>.
    ///
    /// Se comparan números y nunca texto: «1.10.0» es *menor* que «1.9.0» alfabéticamente, y con
    /// una comparación de cadenas el aviso dejaría de salir a partir de la décima entrega.
    /// </summary>
    private static Version? Interpretar(string? etiqueta)
    {
        if (string.IsNullOrWhiteSpace(etiqueta))
        {
            return null;
        }

        string limpia = etiqueta.Trim();
        if (limpia.StartsWith('v') || limpia.StartsWith('V'))
        {
            limpia = limpia[1..];
        }

        int corte = limpia.IndexOfAny(['+', '-']);
        if (corte > 0)
        {
            limpia = limpia[..corte];
        }

        return Version.TryParse(limpia, out var version) ? version : null;
    }
}
