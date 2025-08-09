using System;
using Domain.Enums;
namespace Application.DTOs
{
	public class UpdateEmployeeDto
	{

        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; } 
       public EmpStatus Status { get; set; } = EmpStatus.Active;
        public Guid DepartmentId { get; set; }

    }
}

