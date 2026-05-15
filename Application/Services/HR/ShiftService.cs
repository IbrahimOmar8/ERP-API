using System.Globalization;
using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Models.HR;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.HR
{
    public class ShiftService : IShiftService
    {
        private readonly ApplicationDbContext _context;
        public ShiftService(ApplicationDbContext context) => _context = context;

        public async Task<List<ShiftDto>> GetAllAsync(CancellationToken ct = default) =>
            await _context.Shifts
                .OrderBy(s => s.StartTime)
                .Select(s => Map(s))
                .ToListAsync(ct);

        public async Task<ShiftDto> CreateAsync(CreateShiftDto dto, CancellationToken ct = default)
        {
            var s = new Shift
            {
                Name = dto.Name,
                StartTime = ParseTime(dto.StartTime),
                EndTime = ParseTime(dto.EndTime),
                DaysMask = dto.DaysMask,
                GraceMinutes = dto.GraceMinutes,
                StandardHours = dto.StandardHours,
                OvertimeMultiplier = dto.OvertimeMultiplier,
                LatePenaltyPerMinute = dto.LatePenaltyPerMinute,
                IsActive = dto.IsActive,
            };
            _context.Shifts.Add(s);
            await _context.SaveChangesAsync(ct);
            return Map(s);
        }

        public async Task<ShiftDto?> UpdateAsync(Guid id, CreateShiftDto dto, CancellationToken ct = default)
        {
            var s = await _context.Shifts.FindAsync(new object?[] { id }, ct);
            if (s == null) return null;
            s.Name = dto.Name;
            s.StartTime = ParseTime(dto.StartTime);
            s.EndTime = ParseTime(dto.EndTime);
            s.DaysMask = dto.DaysMask;
            s.GraceMinutes = dto.GraceMinutes;
            s.StandardHours = dto.StandardHours;
            s.OvertimeMultiplier = dto.OvertimeMultiplier;
            s.LatePenaltyPerMinute = dto.LatePenaltyPerMinute;
            s.IsActive = dto.IsActive;
            await _context.SaveChangesAsync(ct);
            return Map(s);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var s = await _context.Shifts.FindAsync(new object?[] { id }, ct);
            if (s == null) return false;
            if (await _context.ShiftAssignments.AnyAsync(a => a.ShiftId == id, ct))
                throw new InvalidOperationException("لا يمكن حذف شيفت مرتبط بموظفين");
            _context.Shifts.Remove(s);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<ShiftAssignmentDto>> GetAssignmentsAsync(Guid? employeeId, CancellationToken ct = default)
        {
            var q = _context.ShiftAssignments.AsQueryable();
            if (employeeId.HasValue) q = q.Where(a => a.EmployeeId == employeeId.Value);

            var rows = await q
                .OrderByDescending(a => a.EffectiveFrom)
                .Select(a => new
                {
                    a.Id, a.EmployeeId, a.ShiftId, a.EffectiveFrom, a.EffectiveTo, a.Notes,
                    EmployeeName = _context.Employees.Where(e => e.Id == a.EmployeeId).Select(e => e.Name).FirstOrDefault(),
                    ShiftName = _context.Shifts.Where(s => s.Id == a.ShiftId).Select(s => s.Name).FirstOrDefault(),
                })
                .ToListAsync(ct);
            return rows.Select(r => new ShiftAssignmentDto
            {
                Id = r.Id, EmployeeId = r.EmployeeId, ShiftId = r.ShiftId,
                EffectiveFrom = r.EffectiveFrom, EffectiveTo = r.EffectiveTo, Notes = r.Notes,
                EmployeeName = r.EmployeeName, ShiftName = r.ShiftName,
            }).ToList();
        }

        public async Task<ShiftAssignmentDto> AssignAsync(CreateShiftAssignmentDto dto, CancellationToken ct = default)
        {
            // Close any open assignment for this employee
            var open = await _context.ShiftAssignments
                .Where(a => a.EmployeeId == dto.EmployeeId && a.EffectiveTo == null)
                .ToListAsync(ct);
            foreach (var o in open) o.EffectiveTo = dto.EffectiveFrom ?? DateTime.UtcNow;

            var a = new ShiftAssignment
            {
                EmployeeId = dto.EmployeeId,
                ShiftId = dto.ShiftId,
                EffectiveFrom = dto.EffectiveFrom ?? DateTime.UtcNow,
                EffectiveTo = dto.EffectiveTo,
                Notes = dto.Notes,
            };
            _context.ShiftAssignments.Add(a);
            await _context.SaveChangesAsync(ct);
            return (await GetAssignmentsAsync(dto.EmployeeId, ct)).First(x => x.Id == a.Id);
        }

        public async Task<bool> RemoveAssignmentAsync(Guid id, CancellationToken ct = default)
        {
            var a = await _context.ShiftAssignments.FindAsync(new object?[] { id }, ct);
            if (a == null) return false;
            _context.ShiftAssignments.Remove(a);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // Helper for AttendanceService — returns currently active shift for an employee
        public static async Task<Shift?> ResolveActiveShiftAsync(ApplicationDbContext ctx, Guid employeeId, DateTime date, CancellationToken ct = default)
        {
            return await ctx.ShiftAssignments
                .Where(a => a.EmployeeId == employeeId
                            && a.EffectiveFrom <= date
                            && (a.EffectiveTo == null || a.EffectiveTo >= date))
                .OrderByDescending(a => a.EffectiveFrom)
                .Join(ctx.Shifts, a => a.ShiftId, s => s.Id, (a, s) => s)
                .FirstOrDefaultAsync(ct);
        }

        private static TimeSpan ParseTime(string input)
        {
            // Accept HH:mm or HH:mm:ss
            return TimeSpan.ParseExact(input, input.Length <= 5 ? @"hh\:mm" : @"hh\:mm\:ss", CultureInfo.InvariantCulture);
        }

        public static ShiftDto Map(Shift s) => new()
        {
            Id = s.Id, Name = s.Name,
            StartTime = s.StartTime.ToString(@"hh\:mm"),
            EndTime = s.EndTime.ToString(@"hh\:mm"),
            DaysMask = s.DaysMask, GraceMinutes = s.GraceMinutes,
            StandardHours = s.StandardHours, OvertimeMultiplier = s.OvertimeMultiplier,
            LatePenaltyPerMinute = s.LatePenaltyPerMinute, IsActive = s.IsActive,
        };
    }
}
