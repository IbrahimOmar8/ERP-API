using Application.DTOs.Auth;
using Application.Inerfaces.Auth;
using Domain.Models.Auth;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Auth
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;

        public UserService(ApplicationDbContext context) => _context = context;

        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = await _context.Users
                .Include(u => u.UserRoles)!
                .ThenInclude(ur => ur.Role)
                .OrderBy(u => u.UserName)
                .ToListAsync();
            return users.Select(u => AuthService.MapUser(u)).ToList();
        }

        public async Task<UserDto?> GetByIdAsync(Guid id)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)!
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);
            return user == null ? null : AuthService.MapUser(user);
        }

        public async Task<UserDto?> UpdateAsync(Guid id, RegisterDto dto)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return null;

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.DefaultWarehouseId = dto.DefaultWarehouseId;
            user.DefaultCashRegisterId = dto.DefaultCashRegisterId;

            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Replace role assignments
            if (user.UserRoles != null)
                _context.UserRoles.RemoveRange(user.UserRoles);

            if (dto.Roles.Count > 0)
            {
                var roles = await _context.Roles
                    .Where(r => dto.Roles.Contains(r.Name) && r.IsActive)
                    .ToListAsync();
                foreach (var role in roles)
                    _context.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetActiveAsync(Guid id, bool active)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;
            user.IsActive = active;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
