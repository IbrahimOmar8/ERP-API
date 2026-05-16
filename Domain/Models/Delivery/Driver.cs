using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Delivery
{
    public class Driver
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(20)]
        public string? NationalId { get; set; }

        public DriverVehicleType VehicleType { get; set; } = DriverVehicleType.Motorcycle;

        [StringLength(50)]
        public string? VehicleNumber { get; set; }

        // Optional commission per delivery (flat amount)
        public decimal CommissionPerDelivery { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
