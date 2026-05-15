using Application.DTOs.Notifications;
using Application.Inerfaces.Integration;
using Application.Inerfaces.Notifications;
using Domain.Models.Notifications;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Notifications
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IRealtimeBroadcaster _realtime;

        public NotificationService(ApplicationDbContext context, IRealtimeBroadcaster realtime)
        {
            _context = context;
            _realtime = realtime;
        }

        public async Task<List<NotificationDto>> GetForUserAsync(Guid userId, bool unreadOnly, int take, CancellationToken ct = default)
        {
            var roles = await GetUserRolesAsync(userId, ct);
            var q = _context.Notifications.AsQueryable()
                .Where(n => n.UserId == userId
                            || (n.UserId == null && n.Role == null)
                            || (n.Role != null && roles.Contains(n.Role)));
            if (unreadOnly) q = q.Where(n => !n.IsRead);

            return await q
                .OrderByDescending(n => n.CreatedAt)
                .Take(Math.Clamp(take, 1, 100))
                .Select(n => Map(n))
                .ToListAsync(ct);
        }

        public async Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default)
        {
            var roles = await GetUserRolesAsync(userId, ct);
            return await _context.Notifications
                .Where(n => !n.IsRead
                            && (n.UserId == userId
                                || (n.UserId == null && n.Role == null)
                                || (n.Role != null && roles.Contains(n.Role))))
                .CountAsync(ct);
        }

        public async Task<bool> MarkReadAsync(Guid id, Guid userId, CancellationToken ct = default)
        {
            var n = await _context.Notifications.FindAsync(new object?[] { id }, ct);
            if (n == null) return false;
            // Only mark personal or role/broadcast notifs that this user can see
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
        {
            var roles = await GetUserRolesAsync(userId, ct);
            var unread = await _context.Notifications
                .Where(n => !n.IsRead
                            && (n.UserId == userId
                                || (n.UserId == null && n.Role == null)
                                || (n.Role != null && roles.Contains(n.Role))))
                .ToListAsync(ct);
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(ct);
            return unread.Count;
        }

        public async Task<NotificationDto> CreateAsync(CreateNotificationDto dto, CancellationToken ct = default)
        {
            var entity = new Notification
            {
                UserId = dto.UserId,
                Role = dto.Role,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type,
                Link = dto.Link,
                Severity = string.IsNullOrWhiteSpace(dto.Severity) ? "info" : dto.Severity,
            };
            _context.Notifications.Add(entity);
            await _context.SaveChangesAsync(ct);

            // Realtime ping — clients refetch the bell + show toast
            try { await _realtime.BroadcastAsync("notification.new", Map(entity)); }
            catch { /* best effort */ }
            return Map(entity);
        }

        private async Task<List<string>> GetUserRolesAsync(Guid userId, CancellationToken ct) =>
            await _context.UserRoles
                .Where(ur => ur.UserId == userId && ur.Role != null)
                .Select(ur => ur.Role!.Name)
                .ToListAsync(ct);

        private static NotificationDto Map(Notification n) => new()
        {
            Id = n.Id,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            Link = n.Link,
            Severity = n.Severity,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
        };
    }
}
