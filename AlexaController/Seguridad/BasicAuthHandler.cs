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
                                 ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(authenticationOptions, logger, encoder, clock)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options)); // Asegúrate de que no sea nulo
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            // Verifica si _options.Value es nulo (lo cual no debería serlo después de la configuración)
            if (_options.Value == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Configuration missing for BasicAuth"));
            }

            var authorizationHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (authorizationHeader == null || !authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Missing or invalid Authorization header"));
            }

            var encodedCredentials = authorizationHeader.Substring("Basic ".Length).Trim();
            var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var credentials = decodedCredentials.Split(':');

            if (credentials.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Authorization header"));
            }

            var username = credentials[0];
            var password = credentials[1];

            // Verifica el nombre de usuario y la contraseña desde el archivo de configuración
            if (username == _options.Value.Username && password == _options.Value.Password)
            {
                var claims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, username) };
                var identity = new System.Security.Claims.ClaimsIdentity(claims, "BasicAuth");
                var principal = new System.Security.Claims.ClaimsPrincipal(identity);
                var ticket = new AuthenticationTicket(principal, "BasicAuth");

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.Fail("Invalid username or password"));
        }
    }
}