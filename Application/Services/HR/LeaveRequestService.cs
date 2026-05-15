using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Domain.Models.HR;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.HR
{
    public class LeaveRequestService : ILeaveRequestService
    {
        private readonly ApplicationDbContext _context;
        public LeaveRequestService(ApplicationDbContext context) => _context = context;

        public async Task<List<LeaveRequestDto>> GetAllAsync(Guid? employeeId, LeaveStatus? status, CancellationToken ct = default)
        {
            var q = _context.LeaveRequests.AsQueryable();
            if (employeeId.HasValue) q = q.Where(r => r.EmployeeId == employeeId.Value);
            if (status.HasValue) q = q.Where(r => r.Status == status.Value);

            var rows = await q
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    Request = r,
                    EmployeeName = _context.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.Name).FirstOrDefault(),
                })
                .ToListAsync(ct);
            return rows.Select(r => Map(r.Request, r.EmployeeName)).ToList();
        }

        public async Task<LeaveRequestDto> CreateAsync(CreateLeaveRequestDto dto, CancellationToken ct = default)
        {
            if (dto.To < dto.From) throw new InvalidOperationException("تاريخ النهاية قبل البداية");
            var days = CalculateBusinessDays(dto.From, dto.To);

            var r = new LeaveRequest
            {
                EmployeeId = dto.EmployeeId,
                Type = dto.Type,
                From = dto.From.Date,
                To = dto.To.Date,
                Days = days,
                Reason = dto.Reason,
                Status = LeaveStatus.Pending,
            };
            _context.LeaveRequests.Add(r);
            await _context.SaveChangesAsync(ct);
            var name = await _context.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.Name).FirstOrDefaultAsync(ct);
            return Map(r, name);
        }

        public async Task<LeaveRequestDto?> SetStatusAsync(Guid id, LeaveStatus status, Guid? approvedByUserId, CancellationToken ct = default)
        {
            var r = await _context.LeaveRequests.FindAsync(new object?[] { id }, ct);
            if (r == null) return null;
            r.Status = status;
            if (status == LeaveStatus.Approved)
            {
                r.ApprovedAt = DateTime.UtcNow;
                r.ApprovedByUserId = approvedByUserId;

                // Stamp attendance for each day in range as OnLeave (unpaid for Unpaid type)
                for (var d = r.From; d <= r.To; d = d.AddDays(1))
                {
                    if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday) continue;
                    var existing = await _context.AttendanceRecords
                        .FirstOrDefaultAsync(a => a.EmployeeId == r.EmployeeId && a.Date == d.Date, ct);
                    if (existing == null)
                    {
                        _context.AttendanceRecords.Add(new AttendanceRecord
                        {
                            EmployeeId = r.EmployeeId, Date = d.Date,
                            Status = AttendanceStatus.OnLeave,
                            Notes = $"{r.Type}",
                        });
                    }
                    else if (existing.CheckIn == null)
                    {
                        existing.Status = AttendanceStatus.OnLeave;
                        existing.Notes = $"{r.Type}";
                    }
                }
            }
            await _context.SaveChangesAsync(ct);
            var name = await _context.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.Name).FirstOrDefaultAsync(ct);
            return Map(r, name);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var r = await _context.LeaveRequests.FindAsync(new object?[] { id }, ct);
            if (r == null) return false;
            _context.LeaveRequests.Remove(r);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static decimal CalculateBusinessDays(DateTime from, DateTime to)
        {
            var days = 0;
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                if (d.DayOfWeek == DayOfWeek.Friday || d.DayOfWeek == DayOfWeek.Saturday) continue;
                days++;
            }
            return days;
        }

        private static LeaveRequestDto Map(LeaveRequest r, string? employeeName) => new()
        {
            Id = r.Id, EmployeeId = r.EmployeeId, EmployeeName = employeeName,
            Type = r.Type, From = r.From, To = r.To, Days = r.Days,
            Status = r.Status, Reason = r.Reason,
            CreatedAt = r.CreatedAt, ApprovedAt = r.ApprovedAt,
        };
    }
}
