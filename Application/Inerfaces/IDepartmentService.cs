using System;
using Application.DTOs;
using Domain.Models;

namespace Application.Inerfaces
{
	public interface IDepartmentService
	{
        Task<Department> CreateAsync(CreateDepartmentDto request);
        Task<List<DepartmentDto>> GetListAsync(int pageNumber, int pageSize);
        Task<List<DepartmentDto>> GetByIdAsync(Guid? Id);

    }
}

