using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Inerfaces.Integration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace ERPTask.Auth
{
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string Scheme = "ApiKey";
    }

    // Accepts requests with header `X-API-Key: <key>` and resolves them to a
    // ClaimsPrincipal with the API key's name + scope claims.
    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        private readonly IApiKeyService _service;
        private const string HeaderName = "X-API-Key";

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            IApiKeyService service)
            : base(options, logger, encoder)
        {
            _service = service;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(HeaderName, out var headerValues))
                return AuthenticateResult.NoResult();

            var rawKey = headerValues.ToString().Trim();
            if (string.IsNullOrWhiteSpace(rawKey)) return AuthenticateResult.NoResult();

            var ip = Context.Connection.RemoteIpAddress?.ToString();
            var key = await _service.ValidateAsync(rawKey, ip);
            if (key == null) return AuthenticateResult.Fail("Invalid or revoked API key");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, key.Id.ToString()),
                new(ClaimTypes.Name, key.Name),
                new("auth_type", "api_key"),
                new("api_key_id", key.Id.ToString()),
            };
            if (!string.IsNullOrEmpty(key.Scopes))
            {
                foreach (var scope in key.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    claims.Add(new Claim("scope", scope.Trim()));
            }

            var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationOptions.Scheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationOptions.Scheme);
            return AuthenticateResult.Success(ticket);
        }
    }
}
