using System.ComponentModel.DataAnnotations;
using Domain.Models.Inventory;

namespace Domain.Models.POS
{
    public class CashRegister
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;

        public Guid WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<CashSession>? Sessions { get; set; }
    }
}
