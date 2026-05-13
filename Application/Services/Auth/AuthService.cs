using Application.DTOs.Auth;
using Application.Inerfaces.Auth;
using Domain.Models.Auth;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Application.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly int _refreshTokenDays;

        public AuthService(
            ApplicationDbContext context,
            ITokenService tokenService,
            IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _refreshTokenDays = int.TryParse(config["Jwt:RefreshTokenDays"], out var d) ? d : 30;
        }

        public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)!
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == dto.UserName);

            if (user == null || !user.IsActive)
                throw new InvalidOperationException("اسم المستخدم أو كلمة المرور غير صحيحة");

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                throw new InvalidOperationException("اسم المستخدم أو كلمة المرور غير صحيحة");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return await IssueTokensAsync(user);
        }

        public async Task<TokenResponseDto> RefreshAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .Include(t => t.User)!
                .ThenInclude(u => u!.UserRoles)!
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (token == null || !token.IsActive || token.User == null || !token.User.IsActive)
                throw new InvalidOperationException("Refresh token غير صالح");

            // Revoke old token and issue new pair
            token.RevokedAt = DateTime.UtcNow;
            return await IssueTokensAsync(token.User);
        }

        public async Task LogoutAsync(string refreshToken)
        {
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == refreshToken);
            if (token != null && token.RevokedAt == null)
            {
                token.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        public async Task<UserDto> RegisterAsync(RegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.UserName == dto.UserName))
                throw new InvalidOperationException("اسم المستخدم مستخدم بالفعل");

            var user = new User
            {
                UserName = dto.UserName,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                DefaultWarehouseId = dto.DefaultWarehouseId,
                DefaultCashRegisterId = dto.DefaultCashRegisterId,
            };
            _context.Users.Add(user);

            // Assign roles
            if (dto.Roles.Count > 0)
            {
                var roles = await _context.Roles
                    .Where(r => dto.Roles.Contains(r.Name) && r.IsActive)
                    .ToListAsync();
                foreach (var role in roles)
                {
                    _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
                }
            }

            await _context.SaveChangesAsync();
            return (await GetCurrentUserAsync(user.Id))!;
        }

        public async Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
                throw new InvalidOperationException("كلمة المرور الحالية غير صحيحة");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserDto?> GetCurrentUserAsync(Guid userId)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)!
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == userId);
            return user == null ? null : MapUser(user);
        }

        private async Task<TokenResponseDto> IssueTokensAsync(User user)
        {
            var roles = user.UserRoles?
                .Select(ur => ur.Role?.Name ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList() ?? new List<string>();

            var accessToken = _tokenService.GenerateAccessToken(user, roles);
            var refreshTokenValue = _tokenService.GenerateRefreshToken();
            var expiresAt = DateTime.UtcNow.AddDays(_refreshTokenDays);

            _context.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                Token = refreshTokenValue,
                ExpiresAt = expiresAt
            });
            await _context.SaveChangesAsync();

            return new TokenResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshTokenValue,
                ExpiresAt = expiresAt,
                User = MapUser(user, roles)
            };
        }

        internal static UserDto MapUser(User u, List<string>? roles = null) => new()
        {
            Id = u.Id,
            UserName = u.UserName,
            FullName = u.FullName,
            Email = u.Email,
            Phone = u.Phone,
            DefaultWarehouseId = u.DefaultWarehouseId,
            DefaultCashRegisterId = u.DefaultCashRegisterId,
            IsActive = u.IsActive,
            Roles = roles ?? u.UserRoles?
                .Select(ur => ur.Role?.Name ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList() ?? new List<string>()
        };
    }
}
