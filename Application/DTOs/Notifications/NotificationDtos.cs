using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Notifications
{
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Link { get; set; }
        public string Severity { get; set; } = "info";
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateNotificationDto
    {
        // Either set UserId for a single user, Role for a role-broadcast,
        // or leave both null for an all-users broadcast.
        public Guid? UserId { get; set; }
        [StringLength(50)] public string? Role { get; set; }

        [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
        [Required, StringLength(1000)] public string Message { get; set; } = string.Empty;
        [StringLength(50)] public string? Type { get; set; }
        [StringLength(250)] public string? Link { get; set; }
        [StringLength(20)] public string Severity { get; set; } = "info";
    }
}
