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

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Encodings.Web;

namespace AlexaController.Seguridad
{
    public class BasicAuthOptions
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    // Middleware de BasicAuth
    public class BasicAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        private readonly IOptions<BasicAuthOptions> _options;

        public BasicAuthHandler(IOptions<BasicAuthOptions> options, IOptionsMonitor<AuthenticationSchemeOptions> authenticationOptions,
                                 ILoggerFactory logger, UrlEncoder encoder)
            : base(authenticationOptions, logger, encoder)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options)); // Asegúrate de que no sea nulo
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var ip = Request.HttpContext.Connection.RemoteIpAddress;

            if (_options.Value == null)
            {
                Logger.LogError("[SECURITY] Configuración BasicAuth no disponible. IP: {IP}", ip);
                return Task.FromResult(AuthenticateResult.Fail("Configuration missing for BasicAuth"));
            }

            var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (authorizationHeader == null || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogWarning("[SECURITY] Solicitud sin cabecera Authorization válida. IP: {IP}", ip);
                return Task.FromResult(AuthenticateResult.Fail("Missing or invalid Authorization header"));
            }

            var encodedCredentials = authorizationHeader.Substring("Basic ".Length).Trim();
            var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var credentials = decodedCredentials.Split(':');

            if (credentials.Length != 2)
            {
                Logger.LogWarning("[SECURITY] Cabecera Authorization con formato inválido. IP: {IP}", ip);
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));
            }

            var username = credentials[0];
            var password = credentials[1];

            if (username == _options.Value.Username && password == _options.Value.Password)
            {
                Logger.LogInformation("[SECURITY] Autenticación correcta. Usuario: {Username}, IP: {IP}", username, ip);

                var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) };
                var identity = new System.Security.Claims.ClaimsIdentity(claims, "BasicAuth");
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "BasicAuth");

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            Logger.LogWarning("[SECURITY] Intento de autenticación fallido. Usuario: {Username}, IP: {IP}", username, ip);
            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
        }
    }
}