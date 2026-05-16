using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Production
{
    // ─── Bills of Materials ─────────────────────────────────────────────

    public class BomDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal OutputQuantity { get; set; }
        public decimal AdditionalCostPerUnit { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }
        public List<BomComponentDto> Components { get; set; } = new();

        // Total estimated cost of one finished unit (sum of components × average cost + extra)
        public decimal EstimatedUnitCost { get; set; }
    }

    public class BomComponentDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductSku { get; set; }
        public decimal Quantity { get; set; }
        public decimal WastePercent { get; set; }
        public decimal CurrentCost { get; set; }
    }

    public class CreateBomDto
    {
        [Required] public Guid ProductId { get; set; }
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        public decimal OutputQuantity { get; set; } = 1m;
        public decimal AdditionalCostPerUnit { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(500)] public string? Notes { get; set; }
        [Required] public List<CreateBomComponentDto> Components { get; set; } = new();
    }

    public class CreateBomComponentDto
    {
        [Required] public Guid ProductId { get; set; }
        public decimal Quantity { get; set; }
        public decimal WastePercent { get; set; }
    }

    // ─── Production Orders ──────────────────────────────────────────────

    public class ProductionOrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid BillOfMaterialsId { get; set; }
        public string? BomName { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public decimal Quantity { get; set; }
        public decimal TotalCost { get; set; }
        public decimal UnitCost { get; set; }
        public ProductionOrderStatus Status { get; set; }
        public DateTime PlannedDate { get; set; }
        public DateTime? CompletedDate { get; set; }
        public string? Notes { get; set; }
        public List<ProductionOrderItemDto> Items { get; set; } = new();
    }

    public class ProductionOrderItemDto
    {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class CreateProductionOrderDto
    {
        [Required] public Guid BillOfMaterialsId { get; set; }
        [Required] public Guid WarehouseId { get; set; }
        [Required, Range(0.0001, double.MaxValue)] public decimal Quantity { get; set; }
        public DateTime? PlannedDate { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
    }
}
