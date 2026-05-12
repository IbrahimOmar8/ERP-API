using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.POS
{
    public class CashRegisterDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public bool IsActive { get; set; }
        public bool HasOpenSession { get; set; }
    }

    public class CreateCashRegisterDto
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;
        [Required, StringLength(50)]
        public string Code { get; set; } = string.Empty;
        public Guid WarehouseId { get; set; }
    }

    public class CashSessionDto
    {
        public Guid Id { get; set; }
        public Guid CashRegisterId { get; set; }
        public string? CashRegisterName { get; set; }
        public Guid CashierUserId { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal ExpectedBalance { get; set; }
        public decimal Difference { get; set; }
        public decimal TotalCashSales { get; set; }
        public decimal TotalCardSales { get; set; }
        public decimal TotalOtherSales { get; set; }
        public decimal TotalRefunds { get; set; }
        public CashSessionStatus Status { get; set; }
    }

    public class OpenSessionDto
    {
        [Required]
        public Guid CashRegisterId { get; set; }
        public decimal OpeningBalance { get; set; }
    }

    public class CloseSessionDto
    {
        public decimal ClosingBalance { get; set; }
        [StringLength(500)]
        public string? Notes { get; set; }
    }
}
