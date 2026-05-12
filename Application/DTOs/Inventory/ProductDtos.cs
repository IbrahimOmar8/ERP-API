using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Inventory
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string? Barcode { get; set; }
        public string NameAr { get; set; } = string.Empty;
        public string? NameEn { get; set; }
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public Guid UnitId { get; set; }
        public string? UnitName { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinSalePrice { get; set; }
        public decimal VatRate { get; set; }
        public string? ItemCode { get; set; }
        public string? GS1Code { get; set; }
        public decimal MinStockLevel { get; set; }
        public decimal MaxStockLevel { get; set; }
        public bool TrackStock { get; set; }
        public bool IsActive { get; set; }
        public decimal CurrentStock { get; set; }
    }

    public class CreateProductDto
    {
        [Required, StringLength(50)]
        public string Sku { get; set; } = string.Empty;
        [StringLength(50)]
        public string? Barcode { get; set; }
        [Required, StringLength(250)]
        public string NameAr { get; set; } = string.Empty;
        [StringLength(250)]
        public string? NameEn { get; set; }
        [StringLength(1000)]
        public string? Description { get; set; }
        public Guid CategoryId { get; set; }
        public Guid UnitId { get; set; }
        public decimal PurchasePrice { get; set; }
        public decimal SalePrice { get; set; }
        public decimal MinSalePrice { get; set; }
        public decimal VatRate { get; set; } = 14m;
        [StringLength(50)]
        public string? ItemCode { get; set; }
        [StringLength(50)]
        public string? GS1Code { get; set; }
        public decimal MinStockLevel { get; set; }
        public decimal MaxStockLevel { get; set; }
        public bool TrackStock { get; set; } = true;
    }

    public class UpdateProductDto : CreateProductDto
    {
        public bool IsActive { get; set; } = true;
    }

    public class ProductFilterDto
    {
        public string? Search { get; set; }
        public Guid? CategoryId { get; set; }
        public bool? IsActive { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
