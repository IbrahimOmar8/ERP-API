using System.ComponentModel.DataAnnotations;

namespace Domain.Models.HR
{
    public class Shift
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Start/end times as TimeSpan (HH:mm:ss). Stored as ticks/string by EF.
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        // Bit-field of weekdays this shift applies to. Sunday=1, Monday=2, ...
        // Default: Sun–Thu = 1+2+4+8+16 = 31 (EG work week)
        public int DaysMask { get; set; } = 31;

        // Allowed lateness in minutes before "Late" is recorded
        public int GraceMinutes { get; set; } = 10;

        // Hours considered full shift (overtime starts after)
        public decimal StandardHours { get; set; } = 8m;

        // Multiplier applied on overtime hourly rate (1.5 = 150% etc.)
        public decimal OvertimeMultiplier { get; set; } = 1.5m;

        // Penalty per minute past grace (deducted from monthly salary)
        public decimal LatePenaltyPerMinute { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // Assigns an employee to a shift across a date range. Latest assignment wins.
    public class ShiftAssignment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid EmployeeId { get; set; }

        public Guid ShiftId { get; set; }
        public Shift? Shift { get; set; }

        public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
        public DateTime? EffectiveTo { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
