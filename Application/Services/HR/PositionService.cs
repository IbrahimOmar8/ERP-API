using Application.DTOs.HR;
using Application.Inerfaces.HR;
using Domain.Models.HR;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.HR
{
    public class PositionService : IPositionService
    {
        private readonly ApplicationDbContext _context;
        public PositionService(ApplicationDbContext context) => _context = context;

        public async Task<List<PositionDto>> GetAllAsync(CancellationToken ct = default)
        {
            var rows = await _context.Positions
                .OrderBy(p => p.Title)
                .Select(p => new
                {
                    p.Id, p.Title, p.BaseSalary, p.DepartmentId,
                    DeptName = _context.Departments.Where(d => d.Id == p.DepartmentId).Select(d => d.Name).FirstOrDefault(),
                    p.Description, p.IsActive,
                    EmployeeCount = _context.Employees.Count(e => e.PositionId == p.Id),
                })
                .ToListAsync(ct);
            return rows.Select(r => new PositionDto
            {
                Id = r.Id, Title = r.Title, BaseSalary = r.BaseSalary,
                DepartmentId = r.DepartmentId, DepartmentName = r.DeptName,
                Description = r.Description, IsActive = r.IsActive,
                EmployeeCount = r.EmployeeCount,
            }).ToList();
        }

        public async Task<PositionDto> CreateAsync(CreatePositionDto dto, CancellationToken ct = default)
        {
            var p = new Position
            {
                Title = dto.Title, BaseSalary = dto.BaseSalary,
                DepartmentId = dto.DepartmentId, Description = dto.Description,
                IsActive = dto.IsActive,
            };
            _context.Positions.Add(p);
            await _context.SaveChangesAsync(ct);
            return (await GetAllAsync(ct)).First(x => x.Id == p.Id);
        }

        public async Task<PositionDto?> UpdateAsync(Guid id, CreatePositionDto dto, CancellationToken ct = default)
        {
            var p = await _context.Positions.FindAsync(new object?[] { id }, ct);
            if (p == null) return null;
            p.Title = dto.Title; p.BaseSalary = dto.BaseSalary;
            p.DepartmentId = dto.DepartmentId; p.Description = dto.Description;
            p.IsActive = dto.IsActive;
            await _context.SaveChangesAsync(ct);
            return (await GetAllAsync(ct)).FirstOrDefault(x => x.Id == id);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _context.Positions.FindAsync(new object?[] { id }, ct);
            if (p == null) return false;
            if (await _context.Employees.AnyAsync(e => e.PositionId == id, ct))
                throw new InvalidOperationException("لا يمكن حذف وظيفة مرتبطة بموظفين");
            _context.Positions.Remove(p);
            await _context.SaveChangesAsync(ct);
            return true;
        }
    }
}
