using System;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Application.DTOs;
using Domain.Models;
using Application.Inerfaces;


namespace Application.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly ApplicationDbContext _context;

        public DepartmentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Department> CreateAsync(CreateDepartmentDto request)
        {
            var department = new Department
            {
                Name = request.Name,
            };

            _context.Departments.Add(department);
            await _context.SaveChangesAsync();
            return department;
        }

        public async Task<List<DepartmentDto>> GetListAsync(int pageNumber, int pageSize)
        {
            return await _context.Departments
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Employees = d.Employees.Select(e => new EmployeeDto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Email = e.Email,
                        HireDate = e.HireDate,
                        Status = e.Status
                    }).ToList()
                })
                .ToListAsync();
        }
        
        public async Task<List<DepartmentDto>> GetByIdAsync(Guid? Id)
        {
            if (Id == null)
            {
                return new List<DepartmentDto>();
            }

            return await _context.Departments
                .Where(d => d.Id == Id)
                .Select(d => new DepartmentDto
                {
                    Id = d.Id,
                    Name = d.Name,
                    Employees = d.Employees.Select(e => new EmployeeDto
                    {
                        Id = e.Id,
                        Name = e.Name,
                        Email = e.Email,
                        HireDate = e.HireDate,
                        Status = e.Status
                    }).ToList()
                })
                .ToListAsync();
        }
    }
}

