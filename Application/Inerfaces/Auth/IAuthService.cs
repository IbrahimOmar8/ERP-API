using Application.DTOs.Auth;

namespace Application.Inerfaces.Auth
{
    public interface IAuthService
    {
        Task<TokenResponseDto> LoginAsync(LoginDto dto);
        Task<TokenResponseDto> RefreshAsync(string refreshToken);
        Task LogoutAsync(string refreshToken);
        Task<UserDto> RegisterAsync(RegisterDto dto);
        Task<bool> ChangePasswordAsync(Guid userId, ChangePasswordDto dto);
        Task<UserDto?> GetCurrentUserAsync(Guid userId);
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
