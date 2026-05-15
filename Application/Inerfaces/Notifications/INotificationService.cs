using Application.DTOs.Notifications;

namespace Application.Inerfaces.Notifications
{
    public interface INotificationService
    {
        // Notifications for the calling user — includes role-broadcasts +
        // global broadcasts.
        Task<List<NotificationDto>> GetForUserAsync(Guid userId, bool unreadOnly, int take, CancellationToken ct = default);
        Task<int> GetUnreadCountAsync(Guid userId, CancellationToken ct = default);
        Task<bool> MarkReadAsync(Guid id, Guid userId, CancellationToken ct = default);
        Task<int> MarkAllReadAsync(Guid userId, CancellationToken ct = default);

        // Create — also fires SignalR "notification.new" event.
        Task<NotificationDto> CreateAsync(CreateNotificationDto dto, CancellationToken ct = default);
    }
}
