using System;
using Application.DTOs;
using Domain.Models;

namespace Application.Inerfaces
{
	public interface IEmployeeService
	{
        Task<Employee> CreateEmployeeAsync(CreateEmployeeDto Employee);

        Task<Employee> UpdateEmployeeAsync(Guid id ,UpdateEmployeeDto Employee);

        Task<bool> DeleteEmployeeAsync(Guid id);

        Task<EmployeeDto> GetEmployeeByIdAsync(Guid id);
        Task<List<EmployeeDto>> GetEmployeesAsync(FilterEmployeeDto filterEmployee);

    }
}

