using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.HR
{
    public class LeaveRequest
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EmployeeId { get; set; }

        public LeaveType Type { get; set; } = LeaveType.Annual;

        public DateTime From { get; set; }
        public DateTime To { get; set; }

        // Days (computed from From/To exclusive of weekends — service layer fills in)
        public decimal Days { get; set; }

        public LeaveStatus Status { get; set; } = LeaveStatus.Pending;

        [StringLength(500)]
        public string? Reason { get; set; }

        public Guid? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
