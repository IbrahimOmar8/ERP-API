using System;
using Domain.Enums;
namespace Application.DTOs
{
	public class CreateEmployeeDto
	{

        public string Name { get; set; }
        public string Email { get; set; }
        public Guid DepartmentId { get; set; }
      

    }
}

