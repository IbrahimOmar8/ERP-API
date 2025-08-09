using System;
using Domain.Enums;
namespace Application.DTOs
{
	public class EmployeeDto
	{

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; } = DateTime.UtcNow;
        public EmpStatus Status { get; set; } = EmpStatus.Active;
        public Guid DepartmentId { get; set; }
        public DepartmentDto Department { get; set; }

    }
}

