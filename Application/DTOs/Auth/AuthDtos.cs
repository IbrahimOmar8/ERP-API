using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Auth
{
    public class LoginDto
    {
        [Required, StringLength(50)]
        public string UserName { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterDto
    {
        [Required, StringLength(50)]
        public string UserName { get; set; } = string.Empty;
        [Required, StringLength(150)]
        public string FullName { get; set; } = string.Empty;
        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
        [StringLength(100)]
        public string? Email { get; set; }
        [StringLength(50)]
        public string? Phone { get; set; }
        public Guid? DefaultWarehouseId { get; set; }
        public Guid? DefaultCashRegisterId { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class TokenResponseDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public UserDto User { get; set; } = new();
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public Guid? DefaultWarehouseId { get; set; }
        public Guid? DefaultCashRegisterId { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
    }

    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class ChangePasswordDto
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
        [Required, MinLength(6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
