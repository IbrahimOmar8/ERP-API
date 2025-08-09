using System;
using System.ComponentModel.DataAnnotations;

namespace Domain.Models
{
    public class Department
    {
        [Key]
        public Guid Id { get; set; } = new Guid();
        [Required, StringLength(100)]
        public string Name { get; set; }


        public ICollection<Employee>? Employees { get; set; }

    }
}

