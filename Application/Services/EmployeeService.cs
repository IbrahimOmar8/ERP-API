using System;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Application.Inerfaces;
using Domain.Models;
using Application.DTOs;
using Domain.Enums;
//using AutoMapper;
namespace Application.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly ApplicationDbContext _context;
         private readonly ILogHistoryService _logHistoryService;
        public EmployeeService(ApplicationDbContext context , ILogHistoryService logHistoryService) 
        {
            _context = context;
            _logHistoryService = logHistoryService;
        }

        public async Task<Employee> CreateEmployeeAsync(CreateEmployeeDto employeeDto)
        {
            var employee = new Employee
            {
                Name = employeeDto.Name,
                Email = employeeDto.Email,
                HireDate = DateTime.UtcNow,
                Status = EmpStatus.Active,
                DepartmentId = employeeDto.DepartmentId
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();
             // Log the create action
        await _logHistoryService.LogCreateAsync(
            nameof(CreateEmployeeDto), 
            employee.Id.ToString(), 
            employee.Name);

            

            return employee;
        }

        public async Task<List<EmployeeDto>> GetEmployeesAsync(FilterEmployeeDto filterEmployee)
        {
            var query = _context.Employees.AsQueryable();
            var searchTerm = filterEmployee.search?.Trim();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(e => e.Name.Contains(searchTerm) || e.Email.Contains(searchTerm));
            }
            if (filterEmployee.status.HasValue)
            {
                query = query.Where(e => e.Status == filterEmployee.status.Value);
            }

            if (filterEmployee.departmentId.HasValue)
            {
                query = query.Where(e => e.DepartmentId == filterEmployee.departmentId.Value);
            }
            if (filterEmployee.hireDateFrom.HasValue)
            {
                query = query.Where(e => e.HireDate >= filterEmployee.hireDateFrom.Value);
            }
            if (filterEmployee.hireDateTo.HasValue)
            {
                query = query.Where(e => e.HireDate <= filterEmployee.hireDateTo.Value);
            }

            query = query.Skip((filterEmployee.pageNumber - 1) * filterEmployee.pageSize)
                         .Take(filterEmployee.pageSize);
            if (!string.IsNullOrEmpty(filterEmployee.sortBy))
            {
                if (filterEmployee.sortDirection.ToLower() == "desc")
                {
                    query = query.OrderByDescending(e => EF.Property<object>(e, filterEmployee.sortBy));
                }
                else
                {
                    query = query.OrderBy(e => EF.Property<object>(e, filterEmployee.sortBy));
                }
            }

            return await query.Select(e => new EmployeeDto
            {
                Id = e.Id,
                Name = e.Name,
                Email = e.Email,
                HireDate = e.HireDate,
                Status = e.Status,
                DepartmentId = e.DepartmentId,
                Department = new DepartmentDto
                {
                    Id = e.Department.Id,
                    Name = e.Department.Name
                }
            }).ToListAsync();
        }
        public async Task<EmployeeDto> GetEmployeeByIdAsync(Guid id)
        {
            var employee = await _context.Employees
                .Include(e => e.Department)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (employee == null) return null;

            return new EmployeeDto
            {
                Id = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                HireDate = employee.HireDate,
                Status = employee.Status,
                DepartmentId = employee.DepartmentId,
                Department = new DepartmentDto
                {
                    Id = employee.Department.Id,
                    Name = employee.Department.Name
                }
            };
        }

        public async Task<Employee> UpdateEmployeeAsync(Guid id, UpdateEmployeeDto employeeDto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return null;

            employee.Name = employeeDto.Name;
            employee.Email = employeeDto.Email;
            employee.Status = employeeDto.Status;
            employee.DepartmentId = employeeDto.DepartmentId;

            _context.Employees.Update(employee);
            await _context.SaveChangesAsync();

             await _logHistoryService.LogCreateAsync(
            nameof(UpdateEmployeeDto), 
            employee.Id.ToString(), 
            employee.Name);


            return employee;
        }

        public async Task<bool> DeleteEmployeeAsync(Guid id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return false;

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();
            return true;
        }
        
        
        
        
    }
}

