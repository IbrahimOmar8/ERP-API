using System;
using Domain.Enums;
namespace Application.DTOs
{
	public class FilterEmployeeDto
	{

        public string? search { get; set; } // Search by name or email
        public EmpStatus? status { get; set; } // Filter by status
        public Guid? departmentId { get; set; } // Filter by department ID
        public DateTime? hireDateFrom { get; set; } // Filter by hire date start
        public DateTime? hireDateTo { get; set; } // Filter by hire date end
        
        public int pageNumber { get; set; } = 1; // Pagination
        public int pageSize { get; set; } = 10; // Pagination
        public string sortBy { get; set; } = "Name"; // Default sorting by Name
        public string sortDirection { get; set; } = "asc"; // Default sorting direction

    }
}

