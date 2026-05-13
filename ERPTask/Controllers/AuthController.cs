using System.Security.Claims;
using Application.DTOs.Auth;
using Application.Inerfaces.Auth;
using Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ERPTask.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService) => _authService = authService;

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            try { return Ok(await _authService.LoginAsync(dto)); }
            catch (InvalidOperationException ex) { return Unauthorized(new { error = ex.Message }); }
        }

        [AllowAnonymous]
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
    }
}
