using System.Security.Claims;
using Domain.Models.Auth;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ERPTask.Controllers
{
    [Route("")]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context) => _context = context;

        [HttpPost("login-cookie")]
        public async Task<IActionResult> LoginCookie([FromForm] string userName, [FromForm] string password, [FromForm] string? returnUrl)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)!.ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == userName);

            if (user == null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return Redirect($"/login?error=1{(string.IsNullOrEmpty(returnUrl) ? "" : "&returnUrl=" + Uri.EscapeDataString(returnUrl))}");

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName),
                new("uname", user.UserName)
            };
            foreach (var ur in user.UserRoles ?? Enumerable.Empty<UserRole>())
                if (ur.Role != null) claims.Add(new Claim(ClaimTypes.Role, ur.Role.Name));

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                new AuthenticationProperties { IsPersistent = true });

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return Redirect(string.IsNullOrEmpty(returnUrl) ? "/dashboard" : returnUrl);
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/login");
        }
    }
}
