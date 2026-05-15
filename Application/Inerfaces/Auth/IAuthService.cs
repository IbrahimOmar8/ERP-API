using Application.DTOs.Auth;

namespace Application.Inerfaces.Auth
{
    public interface IAuthService
    {
        Task<TokenResponseDto> LoginAsync(LoginDto dto);
        Task<TokenResponseDto> LoginWithTwoFactorAsync(TwoFactorLoginDto dto);
        Task<TokenResponseDto> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<UserDto> RegisterAsync(RegisterDto dto);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<UserDto?> GetCurrentUserAsync(Guid userId);

        // Two-factor
        Task<Enable2FaInitDto> Init2FaAsync(Guid userId);
        Task<bool> Enable2FaAsync(Guid userId, string code);
        Task<bool> Disable2FaAsync(Guid userId, string password);

        // Password reset
        Task<ForgotPasswordResultDto> ForgotPasswordAsync(string email);
        Task<bool> ResetPasswordAsync(string token, string newPassword);
    }

    public interface IUserService
    {
        Task<List<UserDto>> GetAllAsync();
        Task<UserDto?> GetByIdAsync(Guid id);
        Task<UserDto?> UpdateAsync(Guid id, RegisterDto dto);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> SetActiveAsync(Guid id, bool active);
    }

    public interface ITokenService
    {
        string GenerateAccessToken(Domain.Models.Auth.User user, IEnumerable<string> roles);
        string GenerateRefreshToken();
    }
}
