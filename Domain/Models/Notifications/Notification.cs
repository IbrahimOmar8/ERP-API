using System.ComponentModel.DataAnnotations;

namespace Domain.Models.Notifications
{
    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // Null = broadcast to everyone (or all users with a specific role)
        public Guid? UserId { get; set; }

        // Optional role-broadcast (e.g. "Admin", "Manager"). When set,
        // the notification is for all users in that role.
        [StringLength(50)]
        public string? Role { get; set; }

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        [Required, StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        // Free-form tag for filtering: "stock.low", "eta.failed", "sale.large", ...
        [StringLength(50)]
        public string? Type { get; set; }

        // Optional deep link the client can navigate to ("/sales/{id}")
        [StringLength(250)]
        public string? Link { get; set; }

        [StringLength(20)]
        public string Severity { get; set; } = "info"; // info | success | warning | error

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
    }
}
