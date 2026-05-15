using System.Security.Claims;
using Application.DTOs.Auth;
using Application.Inerfaces.Auth;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) => _authService = authService;

        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try { return Ok(await _authService.LoginAsync(dto)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto dto)
        {
            try { return Ok(await _authService.RefreshAsync(dto.RefreshToken)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout(RefreshTokenRequestDto dto)
        {
            await _authService.LogoutAsync(dto.RefreshToken);
            return NoContent();
        }

        [Authorize(Roles = Roles.Admin)]
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            try { return Ok(await _authService.RegisterAsync(dto)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<IActionResult> Me()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();
            var user = await _authService.GetCurrentUserAsync(userId);
            return user == null ? NotFound() : Ok(user);
        }

        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordDto dto)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();
            try
            {
                var ok = await _authService.ChangePasswordAsync(userId, dto);
                return ok ? NoContent() : NotFound();
            }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ─── Two-factor authentication ─────────────────────────────────

        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("login-2fa")]
        public async Task<IActionResult> LoginWith2Fa(TwoFactorLoginDto dto)
        {
            try { return Ok(await _authService.LoginWithTwoFactorAsync(dto)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        [Authorize]
        [HttpPost("2fa/init")]
        public async Task<IActionResult> Init2Fa()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();
            try { return Ok(await _authService.Init2FaAsync(userId)); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        [Authorize]
        [HttpPost("2fa/enable")]
        public async Task<IActionResult> Enable2Fa(Enable2FaConfirmDto dto)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();
            try { return Ok(new { enabled = await _authService.Enable2FaAsync(userId, dto.Code) }); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        public record Disable2FaRequest(string Password);

        [Authorize]
        [HttpPost("2fa/disable")]
        public async Task<IActionResult> Disable2Fa(Disable2FaRequest req)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value;
            if (!Guid.TryParse(idClaim, out var userId)) return Unauthorized();
            try { return Ok(new { disabled = await _authService.Disable2FaAsync(userId, req.Password) }); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }

        // ─── Password reset ────────────────────────────────────────────

        [AllowAnonymous]
        [EnableRateLimiting("auth-login")]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
        {
            // Always return 200 to avoid email enumeration
            await _authService.ForgotPasswordAsync(dto.Email);
            return Ok(new { success = true });
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
        {
            try { return Ok(new { success = await _authService.ResetPasswordAsync(dto.Token, dto.NewPassword) }); }
            catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }
        }
    }
}
