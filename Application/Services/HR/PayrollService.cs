using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Domain.Models;
using Domain.Models.HR;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.HR
{
    public class PayrollService : IPayrollService
    {
        private readonly ApplicationDbContext _context;
        public PayrollService(ApplicationDbContext context) => _context = context;

        public async Task<List<PayrollDto>> GetForPeriodAsync(int year, int month, CancellationToken ct = default)
        {
            var rows = await _context.Payrolls
                .Where(p => p.Year == year && p.Month == month)
                .Select(p => new
                {
                    Payroll = p,
                    EmployeeName = _context.Employees.Where(e => e.Id == p.EmployeeId).Select(e => e.Name).FirstOrDefault(),
                })
                .ToListAsync(ct);
            return rows.Select(r => Map(r.Payroll, r.EmployeeName)).ToList();
        }

        public async Task<PayrollDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _context.Payrolls.FindAsync(new object?[] { id }, ct);
            if (p == null) return null;
            var name = await _context.Employees.Where(e => e.Id == p.EmployeeId).Select(e => e.Name).FirstOrDefaultAsync(ct);
            return Map(p, name);
        }

        public async Task<List<PayrollDto>> GenerateAsync(GeneratePayrollDto dto, CancellationToken ct = default)
        {
            // Determine target employees
            var employees = await _context.Employees
                .Where(e => e.Status == EmpStatus.Active
                            && (dto.EmployeeIds == null
                                || dto.EmployeeIds.Count == 0
                                || dto.EmployeeIds.Contains(e.Id)))
                .ToListAsync(ct);

            var monthStart = new DateTime(dto.Year, dto.Month, 1);
            var monthEnd = monthStart.AddMonths(1);
            var workingDaysInMonth = CountBusinessDays(monthStart, monthEnd.AddDays(-1));

            var generated = new List<Payroll>();
            foreach (var emp in employees)
            {
                var existing = await _context.Payrolls
                    .FirstOrDefaultAsync(p => p.EmployeeId == emp.Id && p.Year == dto.Year && p.Month == dto.Month, ct);
                if (existing != null)
                {
                    if (!dto.Overwrite || existing.Status != PayrollStatus.Draft)
                        continue;
                    _context.Payrolls.Remove(existing);
                }

                // Pull attendance for the month
                var attendance = await _context.AttendanceRecords
                    .Where(r => r.EmployeeId == emp.Id && r.Date >= monthStart && r.Date < monthEnd)
                    .ToListAsync(ct);
                var absent = attendance.Count(r => r.Status == AttendanceStatus.Absent);
                var overtimeHours = attendance.Sum(r => r.OvertimeHours);
                var lateMinutes = attendance.Sum(r => r.LateMinutes);

                // Unpaid leaves in the month
                var unpaidLeaveDays = await _context.LeaveRequests
                    .Where(l => l.EmployeeId == emp.Id
                                && l.Status == LeaveStatus.Approved
                                && l.Type == LeaveType.Unpaid
                                && l.From < monthEnd && l.To >= monthStart)
                    .SumAsync(l => (decimal?)l.Days, ct) ?? 0m;

                // Calculations
                var dailyRate = workingDaysInMonth > 0 ? emp.BaseSalary / workingDaysInMonth : 0;
                var hourlyRate = emp.OvertimeHourlyRate > 0
                    ? emp.OvertimeHourlyRate
                    : (workingDaysInMonth > 0 ? dailyRate / 8m : 0);

                var assignedShift = await ShiftService.ResolveActiveShiftAsync(_context, emp.Id, monthStart, ct);
                var otMultiplier = assignedShift?.OvertimeMultiplier ?? 1.5m;
                var latePenaltyPerMin = assignedShift?.LatePenaltyPerMinute ?? 0m;

                var overtimePay = overtimeHours * hourlyRate * otMultiplier;
                var latePenalty = lateMinutes * latePenaltyPerMin;
                var unpaidPenalty = unpaidLeaveDays * dailyRate;
                var absencePenalty = absent * dailyRate;

                var gross = emp.BaseSalary + emp.Allowances + overtimePay + dto.Bonus;
                var totalDeductions = emp.Deductions + latePenalty + unpaidPenalty + absencePenalty
                                      + dto.Tax + dto.InsuranceContribution;
                var net = gross - totalDeductions;

                var payroll = new Payroll
                {
                    EmployeeId = emp.Id,
                    Year = dto.Year,
                    Month = dto.Month,
                    BaseSalary = emp.BaseSalary,
                    Allowances = emp.Allowances,
                    Deductions = emp.Deductions + absencePenalty,
                    OvertimePay = overtimePay,
                    LatePenalty = latePenalty,
                    UnpaidLeavePenalty = unpaidPenalty,
                    Bonus = dto.Bonus,
                    Tax = dto.Tax,
                    InsuranceContribution = dto.InsuranceContribution,
                    WorkingDays = workingDaysInMonth - absent,
                    AbsentDays = absent,
                    OvertimeHours = overtimeHours,
                    LateMinutes = lateMinutes,
                    GrossPay = Math.Round(gross, 2),
                    NetPay = Math.Round(net, 2),
                    Status = PayrollStatus.Draft,
                };
                _context.Payrolls.Add(payroll);
                generated.Add(payroll);
            }

            await _context.SaveChangesAsync(ct);
            return await GetForPeriodAsync(dto.Year, dto.Month, ct);
        }

        public async Task<PayrollDto?> SetStatusAsync(Guid id, PayrollStatus status, Guid? userId, CancellationToken ct = default)
        {
            var p = await _context.Payrolls.FindAsync(new object?[] { id }, ct);
            if (p == null) return null;
            p.Status = status;
            if (status == PayrollStatus.Approved)
            {
                p.ApprovedAt = DateTime.UtcNow;
                p.ApprovedByUserId = userId;
            }
            else if (status == PayrollStatus.Paid)
            {
                p.PaidAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _context.Payrolls.FindAsync(new object?[] { id }, ct);
            if (p == null) return false;
            if (p.Status == PayrollStatus.Paid)
                throw new InvalidOperationException("لا يمكن حذف راتب مدفوع");
            _context.Payrolls.Remove(p);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static int CountBusinessDays(DateTime from, DateTime to)
        {
            var days = 0;
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday) continue;
                days++;
            }
            return days;
        }

        private static PayrollDto Map(Payroll p, string? employeeName) => new()
        {
            Id = p.Id, EmployeeId = p.EmployeeId, EmployeeName = employeeName,
            Year = p.Year, Month = p.Month,
            BaseSalary = p.BaseSalary, Allowances = p.Allowances, Deductions = p.Deductions,
            OvertimePay = p.OvertimePay, LatePenalty = p.LatePenalty, UnpaidLeavePenalty = p.UnpaidLeavePenalty,
            Bonus = p.Bonus, Tax = p.Tax, InsuranceContribution = p.InsuranceContribution,
            WorkingDays = p.WorkingDays, AbsentDays = p.AbsentDays,
            OvertimeHours = p.OvertimeHours, LateMinutes = p.LateMinutes,
            GrossPay = p.GrossPay, NetPay = p.NetPay,
            Status = p.Status, Notes = p.Notes,
            ApprovedAt = p.ApprovedAt, PaidAt = p.PaidAt,
        };
    }
}
