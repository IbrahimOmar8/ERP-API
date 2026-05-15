using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Domain.Models.HR;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.HR
{
    public class AttendanceService : IAttendanceService
    {
        private readonly ApplicationDbContext _context;
        public AttendanceService(ApplicationDbContext context) => _context = context;

        public async Task<AttendanceDto> CheckInAsync(CheckInDto dto, CancellationToken ct = default)
        {
            var at = (dto.At ?? DateTime.UtcNow);
            var date = at.Date;
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(r => r.EmployeeId == dto.EmployeeId && r.Date == date, ct);
            if (record != null && record.CheckIn != null)
                throw new InvalidOperationException("الموظف سجّل دخول اليوم بالفعل");

            var shift = await ShiftService.ResolveActiveShiftAsync(_context, dto.EmployeeId, at, ct);
            var lateMinutes = ComputeLateMinutes(shift, at);

            if (record == null)
            {
                record = new AttendanceRecord
                {
                    EmployeeId = dto.EmployeeId,
                    Date = date,
                    ShiftId = shift?.Id,
                    CheckIn = at,
                    LateMinutes = lateMinutes,
                    Status = lateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present,
                };
                _context.AttendanceRecords.Add(record);
            }
            else
            {
                record.CheckIn = at;
                record.ShiftId ??= shift?.Id;
                record.LateMinutes = lateMinutes;
                record.Status = lateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present;
                record.UpdatedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync(ct);
            return await ResolveDtoAsync(record, ct);
        }

        public async Task<AttendanceDto?> CheckOutAsync(CheckOutDto dto, CancellationToken ct = default)
        {
            var at = (dto.At ?? DateTime.UtcNow);
            var date = at.Date;
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(r => r.EmployeeId == dto.EmployeeId && r.Date == date, ct);
            if (record == null || record.CheckIn == null)
                throw new InvalidOperationException("لا يوجد تسجيل دخول لليوم");

            record.CheckOut = at;
            record.UpdatedAt = DateTime.UtcNow;

            var shift = record.ShiftId.HasValue
                ? await _context.Shifts.FindAsync(new object?[] { record.ShiftId.Value }, ct)
                : null;
            ComputeWorked(record, shift);

            await _context.SaveChangesAsync(ct);
            return await ResolveDtoAsync(record, ct);
        }

        public async Task<AttendanceDto> UpsertManualAsync(ManualAttendanceDto dto, CancellationToken ct = default)
        {
            var date = dto.Date.Date;
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(r => r.EmployeeId == dto.EmployeeId && r.Date == date, ct);

            var shift = await ShiftService.ResolveActiveShiftAsync(_context, dto.EmployeeId, date, ct);

            if (record == null)
            {
                record = new AttendanceRecord
                {
                    EmployeeId = dto.EmployeeId,
                    Date = date,
                    ShiftId = shift?.Id,
                };
                _context.AttendanceRecords.Add(record);
            }
            record.CheckIn = dto.CheckIn;
            record.CheckOut = dto.CheckOut;
            record.Notes = dto.Notes;
            record.LateMinutes = dto.CheckIn.HasValue ? ComputeLateMinutes(shift, dto.CheckIn.Value) : 0;

            if (dto.CheckIn.HasValue && dto.CheckOut.HasValue && shift != null)
                ComputeWorked(record, shift);
            else { record.WorkedHours = 0; record.OvertimeHours = 0; record.EarlyLeaveMinutes = 0; }

            record.Status = dto.Status ?? (dto.CheckIn.HasValue
                ? (record.LateMinutes > 0 ? AttendanceStatus.Late : AttendanceStatus.Present)
                : AttendanceStatus.Absent);
            record.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);
            return await ResolveDtoAsync(record, ct);
        }

        public async Task<List<AttendanceDto>> GetAsync(AttendanceFilterDto filter, CancellationToken ct = default)
        {
            var q = _context.AttendanceRecords.AsQueryable();
            if (filter.EmployeeId.HasValue) q = q.Where(r => r.EmployeeId == filter.EmployeeId.Value);
            if (filter.From.HasValue) q = q.Where(r => r.Date >= filter.From.Value.Date);
            if (filter.To.HasValue) q = q.Where(r => r.Date <= filter.To.Value.Date);
            if (filter.Status.HasValue) q = q.Where(r => r.Status == filter.Status.Value);

            var rows = await q
                .OrderByDescending(r => r.Date)
                .Select(r => new
                {
                    Record = r,
                    EmployeeName = _context.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.Name).FirstOrDefault(),
                    ShiftName = _context.Shifts.Where(s => s.Id == r.ShiftId).Select(s => s.Name).FirstOrDefault(),
                })
                .ToListAsync(ct);
            return rows.Select(r => Map(r.Record, r.EmployeeName, r.ShiftName)).ToList();
        }

        public async Task<List<AttendanceSummaryDto>> GetSummaryAsync(DateTime from, DateTime to, CancellationToken ct = default)
        {
            var rows = await _context.AttendanceRecords
                .Where(r => r.Date >= from.Date && r.Date <= to.Date)
                .GroupBy(r => r.EmployeeId)
                .Select(g => new
                {
                    EmployeeId = g.Key,
                    Present = g.Count(r => r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late),
                    Absent = g.Count(r => r.Status == AttendanceStatus.Absent),
                    Late = g.Count(r => r.Status == AttendanceStatus.Late),
                    Worked = g.Sum(r => r.WorkedHours),
                    Overtime = g.Sum(r => r.OvertimeHours),
                    LateMin = g.Sum(r => r.LateMinutes),
                })
                .ToListAsync(ct);

            var ids = rows.Select(r => r.EmployeeId).ToList();
            var names = await _context.Employees.Where(e => ids.Contains(e.Id))
                .ToDictionaryAsync(e => e.Id, e => e.Name, ct);

            return rows.Select(r => new AttendanceSummaryDto
            {
                EmployeeId = r.EmployeeId,
                EmployeeName = names.GetValueOrDefault(r.EmployeeId) ?? "—",
                PresentDays = r.Present,
                AbsentDays = r.Absent,
                LateDays = r.Late,
                TotalWorkedHours = r.Worked,
                TotalOvertimeHours = r.Overtime,
                TotalLateMinutes = r.LateMin,
            }).OrderByDescending(r => r.PresentDays).ToList();
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var r = await _context.AttendanceRecords.FindAsync(new object?[] { id }, ct);
            if (r == null) return false;
            _context.AttendanceRecords.Remove(r);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        // ─── helpers ────────────────────────────────────────────────────

        private static int ComputeLateMinutes(Shift? shift, DateTime checkIn)
        {
            if (shift == null) return 0;
            var scheduled = checkIn.Date + shift.StartTime;
            var diff = (int)(checkIn - scheduled).TotalMinutes;
            if (diff <= shift.GraceMinutes) return 0;
            return diff;
        }

        private static void ComputeWorked(AttendanceRecord r, Shift? shift)
        {
            if (r.CheckIn == null || r.CheckOut == null) return;
            var hours = (decimal)(r.CheckOut.Value - r.CheckIn.Value).TotalHours;
            if (hours < 0) hours = 0;
            r.WorkedHours = Math.Round(hours, 2);

            if (shift == null)
            {
                r.OvertimeHours = 0;
                r.EarlyLeaveMinutes = 0;
                return;
            }
            var std = shift.StandardHours;
            r.OvertimeHours = Math.Round(Math.Max(0, hours - std), 2);

            // Early leave: minutes before scheduled end (only if worked less than standard)
            if (hours < std)
            {
                var scheduledEnd = r.Date + shift.EndTime;
                var earlyMin = (int)(scheduledEnd - r.CheckOut.Value).TotalMinutes;
                r.EarlyLeaveMinutes = earlyMin > 0 ? earlyMin : 0;
            }
            else
            {
                r.EarlyLeaveMinutes = 0;
            }
        }

        private async Task<AttendanceDto> ResolveDtoAsync(AttendanceRecord r, CancellationToken ct)
        {
            var employeeName = await _context.Employees.Where(e => e.Id == r.EmployeeId).Select(e => e.Name).FirstOrDefaultAsync(ct);
            var shiftName = r.ShiftId.HasValue
                ? await _context.Shifts.Where(s => s.Id == r.ShiftId.Value).Select(s => s.Name).FirstOrDefaultAsync(ct)
                : null;
            return Map(r, employeeName, shiftName);
        }

        private static AttendanceDto Map(AttendanceRecord r, string? employeeName, string? shiftName) => new()
        {
            Id = r.Id,
            EmployeeId = r.EmployeeId, EmployeeName = employeeName,
            Date = r.Date,
            CheckIn = r.CheckIn, CheckOut = r.CheckOut,
            ShiftId = r.ShiftId, ShiftName = shiftName,
            WorkedHours = r.WorkedHours, OvertimeHours = r.OvertimeHours,
            LateMinutes = r.LateMinutes, EarlyLeaveMinutes = r.EarlyLeaveMinutes,
            Status = r.Status, Notes = r.Notes,
        };
    }
}
