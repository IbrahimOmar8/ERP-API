using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.HR
{
    // ─── Positions ───────────────────────────────────────────────────────

    public class PositionDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public Guid? DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class CreatePositionDto
    {
        [Required, StringLength(150)] public string Title { get; set; } = string.Empty;
        public decimal BaseSalary { get; set; }
        public Guid? DepartmentId { get; set; }
        [StringLength(500)] public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    // ─── Employees (expanded) ────────────────────────────────────────────

    public class EmployeeFullDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
        public string? Address { get; set; }
        public string? PhotoUrl { get; set; }
        public DateTime HireDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public EmpStatus Status { get; set; }
        public Guid DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public Guid? PositionId { get; set; }
        public string? PositionTitle { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal OvertimeHourlyRate { get; set; }
        public string? BankName { get; set; }
        public string? BankAccount { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateEmployeeFullDto
    {
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        [StringLength(150)] public string? Email { get; set; }
        [StringLength(50)] public string? Phone { get; set; }
        [StringLength(20)] public string? NationalId { get; set; }
        [StringLength(500)] public string? Address { get; set; }
        [StringLength(500)] public string? PhotoUrl { get; set; }
        public DateTime? HireDate { get; set; }
        public EmpStatus Status { get; set; } = EmpStatus.Active;
        public Guid DepartmentId { get; set; }
        public Guid? PositionId { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal OvertimeHourlyRate { get; set; }
        [StringLength(100)] public string? BankName { get; set; }
        [StringLength(50)] public string? BankAccount { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
    }

    // ─── Shifts ──────────────────────────────────────────────────────────

    public class ShiftDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string StartTime { get; set; } = "00:00"; // HH:mm
        public string EndTime { get; set; } = "00:00";
        public int DaysMask { get; set; }
        public int GraceMinutes { get; set; }
        public decimal StandardHours { get; set; }
        public decimal OvertimeMultiplier { get; set; }
        public decimal LatePenaltyPerMinute { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateShiftDto
    {
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Required] public string StartTime { get; set; } = "09:00";
        [Required] public string EndTime { get; set; } = "17:00";
        public int DaysMask { get; set; } = 31;
        public int GraceMinutes { get; set; } = 10;
        public decimal StandardHours { get; set; } = 8m;
        public decimal OvertimeMultiplier { get; set; } = 1.5m;
        public decimal LatePenaltyPerMinute { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ShiftAssignmentDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public Guid ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateShiftAssignmentDto
    {
        [Required] public Guid EmployeeId { get; set; }
        [Required] public Guid ShiftId { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
    }

    // ─── Attendance ──────────────────────────────────────────────────────

    public class AttendanceDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public DateTime Date { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public Guid? ShiftId { get; set; }
        public string? ShiftName { get; set; }
        public decimal WorkedHours { get; set; }
        public decimal OvertimeHours { get; set; }
        public int LateMinutes { get; set; }
        public int EarlyLeaveMinutes { get; set; }
        public AttendanceStatus Status { get; set; }
        public string? Notes { get; set; }
    }

    public class CheckInDto
    {
        [Required] public Guid EmployeeId { get; set; }
        public DateTime? At { get; set; }
    }

    public class CheckOutDto
    {
        [Required] public Guid EmployeeId { get; set; }
        public DateTime? At { get; set; }
    }

    public class ManualAttendanceDto
    {
        [Required] public Guid EmployeeId { get; set; }
        [Required] public DateTime Date { get; set; }
        public DateTime? CheckIn { get; set; }
        public DateTime? CheckOut { get; set; }
        public AttendanceStatus? Status { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
    }

    public class AttendanceFilterDto
    {
        public Guid? EmployeeId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public AttendanceStatus? Status { get; set; }
    }

    public class AttendanceSummaryDto
    {
        public Guid EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public int PresentDays { get; set; }
        public int AbsentDays { get; set; }
        public int LateDays { get; set; }
        public decimal TotalWorkedHours { get; set; }
        public decimal TotalOvertimeHours { get; set; }
        public int TotalLateMinutes { get; set; }
    }

    // ─── Leaves ──────────────────────────────────────────────────────────

    public class LeaveRequestDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public LeaveType Type { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal Days { get; set; }
        public LeaveStatus Status { get; set; }
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
    }

    public class CreateLeaveRequestDto
    {
        [Required] public Guid EmployeeId { get; set; }
        public LeaveType Type { get; set; } = LeaveType.Annual;
        [Required] public DateTime From { get; set; }
        [Required] public DateTime To { get; set; }
        [StringLength(500)] public string? Reason { get; set; }
    }

    // ─── Payroll ─────────────────────────────────────────────────────────

    public class PayrollDto
    {
        public Guid Id { get; set; }
        public Guid EmployeeId { get; set; }
        public string? EmployeeName { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal BaseSalary { get; set; }
        public decimal Allowances { get; set; }
        public decimal Deductions { get; set; }
        public decimal OvertimePay { get; set; }
        public decimal LatePenalty { get; set; }
        public decimal UnpaidLeavePenalty { get; set; }
        public decimal Bonus { get; set; }
        public decimal Tax { get; set; }
        public decimal InsuranceContribution { get; set; }
        public int WorkingDays { get; set; }
        public int AbsentDays { get; set; }
        public decimal OvertimeHours { get; set; }
        public int LateMinutes { get; set; }
        public decimal GrossPay { get; set; }
        public decimal NetPay { get; set; }
        public PayrollStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class GeneratePayrollDto
    {
        [Required, Range(2000, 2100)] public int Year { get; set; }
        [Required, Range(1, 12)] public int Month { get; set; }
        // If null, generate for all active employees
        public List<Guid>? EmployeeIds { get; set; }
        public decimal Bonus { get; set; }
        public decimal Tax { get; set; }
        public decimal InsuranceContribution { get; set; }
        public bool Overwrite { get; set; } = false; // overwrite existing draft for the period
    }
}
