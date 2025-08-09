using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Domain.Enums;

namespace Domain.Models
{
    public class Employee
    {
        [Key]
        public Guid Id { get; set; } = new Guid();
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime HireDate { get; set; }  = DateTime.UtcNow;
        public EmpStatus Status { get; set; } = EmpStatus.Active;

        public Guid DepartmentId { get; set; }
        public Department Department { get; set; }
    }
}


