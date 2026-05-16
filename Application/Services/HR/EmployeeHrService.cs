using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Enums;
using Domain.Models;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.HR
{
    public class EmployeeHrService : IEmployeeHrService
    {
        private readonly ApplicationDbContext _context;
        public EmployeeHrService(ApplicationDbContext context) => _context = context;

        public async Task<List<EmployeeFullDto>> GetAllAsync(EmpStatus? status, CancellationToken ct = default)
        {
            var q = _context.Employees.AsQueryable();
            if (status.HasValue) q = q.Where(e => e.Status == status.Value);

            var rows = await q
                .OrderBy(e => e.Name)
                .Select(e => new
                {
                    Employee = e,
                    DeptName = _context.Departments.Where(d => d.Id == e.DepartmentId).Select(d => d.Name).FirstOrDefault(),
                    PosTitle = _context.Positions.Where(p => p.Id == e.PositionId).Select(p => p.Title).FirstOrDefault(),
                })
                .ToListAsync(ct);
            return rows.Select(r => Map(r.Employee, r.DeptName, r.PosTitle)).ToList();
        }

        public async Task<EmployeeFullDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var e = await _context.Employees.FindAsync(new object?[] { id }, ct);
            if (e == null) return null;
            var dept = await _context.Departments.Where(d => d.Id == e.DepartmentId).Select(d => d.Name).FirstOrDefaultAsync(ct);
            var pos = await _context.Positions.Where(p => p.Id == e.PositionId).Select(p => p.Title).FirstOrDefaultAsync(ct);
            return Map(e, dept, pos);
        }

        public async Task<EmployeeFullDto> CreateAsync(CreateEmployeeFullDto dto, CancellationToken ct = default)
        {
            var e = new Employee
            {
                Name = dto.Name, Email = dto.Email, Phone = dto.Phone,
                NationalId = dto.NationalId, Address = dto.Address, PhotoUrl = dto.PhotoUrl,
                HireDate = dto.HireDate ?? DateTime.UtcNow, Status = dto.Status,
                DepartmentId = dto.DepartmentId, PositionId = dto.PositionId,
                BaseSalary = dto.BaseSalary, Allowances = dto.Allowances, Deductions = dto.Deductions,
                OvertimeHourlyRate = dto.OvertimeHourlyRate,
                IsSalesman = dto.IsSalesman, CommissionPercent = dto.CommissionPercent,
                BankName = dto.BankName, BankAccount = dto.BankAccount, Notes = dto.Notes,
            };
            _context.Employees.Add(e);
            await _context.SaveChangesAsync(ct);
            return (await GetByIdAsync(e.Id, ct))!;
        }

        public async Task<EmployeeFullDto?> UpdateAsync(Guid id, CreateEmployeeFullDto dto, CancellationToken ct = default)
        {
            var e = await _context.Employees.FindAsync(new object?[] { id }, ct);
            if (e == null) return null;
            e.Name = dto.Name; e.Email = dto.Email; e.Phone = dto.Phone;
            e.NationalId = dto.NationalId; e.Address = dto.Address; e.PhotoUrl = dto.PhotoUrl;
            e.HireDate = dto.HireDate ?? e.HireDate; e.Status = dto.Status;
            e.DepartmentId = dto.DepartmentId; e.PositionId = dto.PositionId;
            e.BaseSalary = dto.BaseSalary; e.Allowances = dto.Allowances; e.Deductions = dto.Deductions;
            e.OvertimeHourlyRate = dto.OvertimeHourlyRate;
            e.IsSalesman = dto.IsSalesman; e.CommissionPercent = dto.CommissionPercent;
            e.BankName = dto.BankName; e.BankAccount = dto.BankAccount; e.Notes = dto.Notes;
            e.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var e = await _context.Employees.FindAsync(new object?[] { id }, ct);
            if (e == null) return false;
            // Soft-delete via Termination if related data exists
            var hasPayroll = await _context.Payrolls.AnyAsync(p => p.EmployeeId == id, ct);
            if (hasPayroll)
            {
                e.Status = EmpStatus.Inactive;
                e.TerminationDate = DateTime.UtcNow;
                await _context.SaveChangesAsync(ct);
                return true;
            }
            _context.Employees.Remove(e);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        private static EmployeeFullDto Map(Employee e, string? dept, string? pos) => new()
        {
            Id = e.Id, Name = e.Name, Email = e.Email, Phone = e.Phone,
            NationalId = e.NationalId, Address = e.Address, PhotoUrl = e.PhotoUrl,
            HireDate = e.HireDate, TerminationDate = e.TerminationDate, Status = e.Status,
            DepartmentId = e.DepartmentId, DepartmentName = dept,
            PositionId = e.PositionId, PositionTitle = pos,
            BaseSalary = e.BaseSalary, Allowances = e.Allowances, Deductions = e.Deductions,
            OvertimeHourlyRate = e.OvertimeHourlyRate,
            IsSalesman = e.IsSalesman, CommissionPercent = e.CommissionPercent,
            BankName = e.BankName, BankAccount = e.BankAccount, Notes = e.Notes,
        };
    }
}
