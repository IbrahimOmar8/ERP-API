using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.HR
{
    public class AttendanceRecord
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EmployeeId { get; set; }

        // Date (yyyy-MM-dd) — uniqueness is on (EmployeeId, Date)
        public DateTime Date { get; set; }

        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }

        public Guid? ShiftId { get; set; }

        // Computed at check-out time (and recomputable from CheckIn/Out)
        public decimal WorkedHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }

        public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
