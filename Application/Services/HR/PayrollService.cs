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
            var employeeIds = employees.Select(e => e.Id).ToList();

            // Batch-fetch related rows for the whole employee set in one round-trip each
            // — replaces a (employees × 5-query) loop with five fixed queries.
            var existingByEmployee = await _context.Payrolls
                .Where(p => p.Year == dto.Year && p.Month == dto.Month && employeeIds.Contains(p.EmployeeId))
                .ToDictionaryAsync(p => p.EmployeeId, ct);

            var attendanceByEmployee = (await _context.AttendanceRecords
                .Where(r => employeeIds.Contains(r.EmployeeId) && r.Date >= monthStart && r.Date < monthEnd)
                .Select(r => new { r.EmployeeId, r.Status, r.OvertimeHours, r.LateMinutes })
                .ToListAsync(ct))
                .GroupBy(r => r.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var unpaidLeaveByEmployee = (await _context.LeaveRequests
                .Where(l => employeeIds.Contains(l.EmployeeId)
                            && l.Status == LeaveStatus.Approved
                            && l.Type == LeaveType.Unpaid
                            && l.From < monthEnd && l.To >= monthStart)
                .Select(l => new { l.EmployeeId, l.Days })
                .ToListAsync(ct))
                .GroupBy(l => l.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Days));

            var loansByEmployee = (await _context.EmployeeLoans
                .Where(l => employeeIds.Contains(l.EmployeeId) && l.Status == EmployeeLoanStatus.Active)
                .ToListAsync(ct))
                .GroupBy(l => l.EmployeeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var shiftByEmployee = await _context.ShiftAssignments
                .Where(a => employeeIds.Contains(a.EmployeeId)
                            && a.EffectiveFrom <= monthStart
                            && (a.EffectiveTo == null || a.EffectiveTo >= monthStart))
                .OrderByDescending(a => a.EffectiveFrom)
                .Join(_context.Shifts, a => a.ShiftId, s => s.Id, (a, s) => new { a.EmployeeId, Shift = s })
                .ToListAsync(ct);
            var shiftByEmployeeMap = shiftByEmployee
                .GroupBy(x => x.EmployeeId)
                .ToDictionary(g => g.Key, g => g.First().Shift);

            var generated = new List<Payroll>();
            foreach (var emp in employees)
            {
                if (existingByEmployee.TryGetValue(emp.Id, out var existing))
                {
                    if (!dto.Overwrite || existing.Status != PayrollStatus.Draft)
                        continue;
                    _context.Payrolls.Remove(existing);
                }

                var attendance = attendanceByEmployee.TryGetValue(emp.Id, out var ar) ? ar : new();
                var absent = attendance.Count(r => r.Status == AttendanceStatus.Absent);
                var overtimeHours = attendance.Sum(r => r.OvertimeHours);
                var lateMinutes = attendance.Sum(r => r.LateMinutes);

                var unpaidLeaveDays = unpaidLeaveByEmployee.TryGetValue(emp.Id, out var ul) ? ul : 0m;

                decimal loanDeduction = 0;
                if (loansByEmployee.TryGetValue(emp.Id, out var activeLoans))
                {
                    foreach (var l in activeLoans)
                    {
                        var remaining = l.Amount - l.AmountRepaid;
                        if (remaining <= 0) continue;
                        var take = Math.Min(l.MonthlyDeduction, remaining);
                        loanDeduction += take;
                        l.AmountRepaid += take;
                        if (l.AmountRepaid >= l.Amount)
                        {
                            l.Status = EmployeeLoanStatus.Completed;
                            l.CompletedDate = DateTime.UtcNow;
                        }
                    }
                }

                // Calculations
                var dailyRate = workingDaysInMonth > 0 ? emp.BaseSalary / workingDaysInMonth : 0;
                var hourlyRate = emp.OvertimeHourlyRate > 0
                    ? emp.OvertimeHourlyRate
                    : (workingDaysInMonth > 0 ? dailyRate / 8m : 0);

                shiftByEmployeeMap.TryGetValue(emp.Id, out var assignedShift);
                var otMultiplier = assignedShift?.OvertimeMultiplier ?? 1.5m;
                var latePenaltyPerMin = assignedShift?.LatePenaltyPerMinute ?? 0m;

                var overtimePay = overtimeHours * hourlyRate * otMultiplier;
                var latePenalty = lateMinutes * latePenaltyPerMin;
                var unpaidPenalty = unpaidLeaveDays * dailyRate;
                var absencePenalty = absent * dailyRate;

                var gross = emp.BaseSalary + emp.Allowances + overtimePay + dto.Bonus;
                var totalDeductions = emp.Deductions + latePenalty + unpaidPenalty + absencePenalty
                                      + dto.Tax + dto.InsuranceContribution + loanDeduction;
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
                    LoanDeduction = loanDeduction,
                    Bonus = dto.Bonus,
                    Tax = dto.Tax,
                    InsuranceContribution = dto.InsuranceContribution,
                    WorkingDays = Math.Max(0, workingDaysInMonth - absent),
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
            LoanDeduction = p.LoanDeduction,
            Bonus = p.Bonus, Tax = p.Tax, InsuranceContribution = p.InsuranceContribution,
            WorkingDays = p.WorkingDays, AbsentDays = p.AbsentDays,
            OvertimeHours = p.OvertimeHours, LateMinutes = p.LateMinutes,
            GrossPay = p.GrossPay, NetPay = p.NetPay,
            Status = p.Status, Notes = p.Notes,
            ApprovedAt = p.ApprovedAt, PaidAt = p.PaidAt,
        };
    }
}
