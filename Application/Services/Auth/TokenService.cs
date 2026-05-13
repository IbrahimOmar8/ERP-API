using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Inerfaces.Auth;
using Domain.Models.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services.Auth
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;

        public TokenService(IConfiguration config) => _config = config;

        public string GenerateAccessToken(User user, IEnumerable<string> roles)
        {
            var jwt = _config.GetSection("Jwt");
            var key = jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured");
            var issuer = jwt["Issuer"];
            var audience = jwt["Audience"];
            var lifetimeMinutes = int.TryParse(jwt["AccessTokenMinutes"], out var m) ? m : 120;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(JwtRegisteredClaimNames.UniqueName, user.UserName),
                new("name", user.FullName),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (user.DefaultWarehouseId.HasValue)
                claims.Add(new Claim("warehouseId", user.DefaultWarehouseId.Value.ToString()));
            if (user.DefaultCashRegisterId.HasValue)
                claims.Add(new Claim("cashRegisterId", user.DefaultCashRegisterId.Value.ToString()));

            foreach (var r in roles)
                claims.Add(new Claim(ClaimTypes.Role, r));

            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(lifetimeMinutes),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        }
    }
}
