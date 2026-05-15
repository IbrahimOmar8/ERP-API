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
        private readonly string _issuer;

        public AuthService(
            ApplicationDbContext context,
            ITokenService tokenService,
            IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _refreshTokenDays = int.TryParse(config["Jwt:RefreshTokenDays"], out var d) ? d : 30;
            _issuer = config["Jwt:Issuer"] ?? "ErpApi";
        }

        public async Task<TokenResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await VerifyCredentialsAsync(dto.UserName, dto.Password);

            if (user.TwoFactorEnabled)
                throw new InvalidOperationException("requires-2fa");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await IssueTokensAsync(user);
        }

        public async Task<TokenResponseDto> LoginWithTwoFactorAsync(TwoFactorLoginDto dto)
        {
            var user = await VerifyCredentialsAsync(dto.UserName, dto.Password);
            if (!user.TwoFactorEnabled || string.IsNullOrEmpty(user.TwoFactorSecret))
                throw new InvalidOperationException("التحقق الثنائي غير مفعّل لهذا الحساب");

            if (!Application.Services.Security.TotpService.Verify(user.TwoFactorSecret, dto.Code))
                throw new InvalidOperationException("رمز التحقق غير صحيح");

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return await IssueTokensAsync(user);
        }

        private async Task<Domain.Models.Auth.User> VerifyCredentialsAsync(string userName, string password)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)!
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null || !user.IsActive)
                throw new InvalidOperationException("اسم المستخدم أو كلمة المرور غير صحيحة");
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new InvalidOperationException("اسم المستخدم أو كلمة المرور غير صحيحة");
            return user;
        }

        public async Task<Enable2FaInitDto> Init2FaAsync(Guid userId)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("المستخدم غير موجود");
            var secret = Application.Services.Security.TotpService.GenerateSecret();
            user.TwoFactorSecret = secret;
            user.TwoFactorEnabled = false; // confirmed by Enable2FaAsync
            await _context.SaveChangesAsync();

            var issuer = _issuer;
            return new Enable2FaInitDto
            {
                Secret = secret,
                OtpAuthUri = Application.Services.Security.TotpService
                    .BuildOtpAuthUri(issuer, user.UserName, secret),
            };
        }

        public async Task<bool> Enable2FaAsync(Guid userId, string code)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("المستخدم غير موجود");
            if (string.IsNullOrEmpty(user.TwoFactorSecret))
                throw new InvalidOperationException("يجب البدء بتفعيل 2FA أولاً");
            if (!Application.Services.Security.TotpService.Verify(user.TwoFactorSecret, code))
                throw new InvalidOperationException("رمز التحقق غير صحيح");
            user.TwoFactorEnabled = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> Disable2FaAsync(Guid userId, string password)
        {
            var user = await _context.Users.FindAsync(userId)
                ?? throw new InvalidOperationException("المستخدم غير موجود");
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                throw new InvalidOperationException("كلمة المرور غير صحيحة");
            user.TwoFactorEnabled = false;
            user.TwoFactorSecret = null;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ForgotPasswordResultDto> ForgotPasswordAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
                return new ForgotPasswordResultDto(); // silent success — avoid enumeration

            var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
                .Replace("+", "-").Replace("/", "_").Replace("=", "");
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpiresAt = DateTime.UtcNow.AddHours(1);
            await _context.SaveChangesAsync();

            // In production, send via email. Returned here so admin tools / dev
            // can complete the flow; the controller hides it by default.
            return new ForgotPasswordResultDto
            {
                Token = token,
                ExpiresAt = user.PasswordResetTokenExpiresAt,
            };
        }

        public async Task<bool> ResetPasswordAsync(string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u =>
                u.PasswordResetToken == token
                && u.PasswordResetTokenExpiresAt != null
                && u.PasswordResetTokenExpiresAt > DateTime.UtcNow);
            if (user == null)
                throw new InvalidOperationException("الرمز غير صالح أو منتهي");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpiresAt = null;
            await _context.SaveChangesAsync();
            return true;
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
