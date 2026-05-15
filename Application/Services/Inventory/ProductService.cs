using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;

        public ProductService(ApplicationDbContext context) => _context = context;

        public async Task<List<ProductDto>> GetAllAsync(ProductFilterDto filter)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(p =>
                    p.NameAr.Contains(s) ||
                    (p.NameEn != null && p.NameEn.Contains(s)) ||
                    p.Sku.Contains(s) ||
                    (p.Barcode != null && p.Barcode.Contains(s)));
            }

            if (filter.CategoryId.HasValue)
                query = query.Where(p => p.CategoryId == filter.CategoryId.Value);

            if (filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            query = query
                .OrderBy(p => p.NameAr)
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize);

            return await query.Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Barcode = p.Barcode,
                NameAr = p.NameAr,
                NameEn = p.NameEn,
                Description = p.Description,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.NameAr : null,
                UnitId = p.UnitId,
                UnitName = p.Unit != null ? p.Unit.NameAr : null,
                PurchasePrice = p.PurchasePrice,
                SalePrice = p.SalePrice,
                MinSalePrice = p.MinSalePrice,
                VatRate = p.VatRate,
                ItemCode = p.ItemCode,
                GS1Code = p.GS1Code,
                MinStockLevel = p.MinStockLevel,
                MaxStockLevel = p.MaxStockLevel,
                TrackStock = p.TrackStock,
                IsActive = p.IsActive,
                ImageUrl = p.ImageUrl,
                CurrentStock = _context.StockItems
                    .Where(s => s.ProductId == p.Id)
                    .Sum(s => (decimal?)s.Quantity) ?? 0
            }).ToListAsync();
        }

        public async Task<ProductDto?> GetByIdAsync(Guid id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.Id == id);

            return product == null ? null : Map(product, await StockSum(id));
        }

        public async Task<ProductDto?> GetByBarcodeAsync(string barcode)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Unit)
                .FirstOrDefaultAsync(p => p.Barcode == barcode);

            return product == null ? null : Map(product, await StockSum(product.Id));
        }

        public async Task<ProductDto> CreateAsync(CreateProductDto dto)
        {
            var product = new Product
            {
                Sku = dto.Sku,
                Barcode = dto.Barcode,
                NameAr = dto.NameAr,
                NameEn = dto.NameEn,
                Description = dto.Description,
                CategoryId = dto.CategoryId,
                UnitId = dto.UnitId,
                PurchasePrice = dto.PurchasePrice,
                SalePrice = dto.SalePrice,
                MinSalePrice = dto.MinSalePrice,
                VatRate = dto.VatRate,
                ItemCode = dto.ItemCode,
                GS1Code = dto.GS1Code,
                MinStockLevel = dto.MinStockLevel,
                MaxStockLevel = dto.MaxStockLevel,
                TrackStock = dto.TrackStock,
                ImageUrl = dto.ImageUrl,
            };
            _context.Products.Add(product);
            await _context.SaveChangesAsync();
            return (await GetByIdAsync(product.Id))!;
        }

        public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return null;

            product.Sku = dto.Sku;
            product.Barcode = dto.Barcode;
            product.NameAr = dto.NameAr;
            product.NameEn = dto.NameEn;
            product.Description = dto.Description;
            product.CategoryId = dto.CategoryId;
            product.UnitId = dto.UnitId;
            product.PurchasePrice = dto.PurchasePrice;
            product.SalePrice = dto.SalePrice;
            product.MinSalePrice = dto.MinSalePrice;
            product.VatRate = dto.VatRate;
            product.ItemCode = dto.ItemCode;
            product.GS1Code = dto.GS1Code;
            product.MinStockLevel = dto.MinStockLevel;
            product.MaxStockLevel = dto.MaxStockLevel;
            product.TrackStock = dto.TrackStock;
            product.IsActive = dto.IsActive;
            product.ImageUrl = dto.ImageUrl;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return false;
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<decimal> StockSum(Guid productId) =>
            await _context.StockItems
                .Where(s => s.ProductId == productId)
                .SumAsync(s => (decimal?)s.Quantity) ?? 0;

        private static ProductDto Map(Product p, decimal currentStock) => new()
        {
            Id = p.Id,
            Sku = p.Sku,
            Barcode = p.Barcode,
            NameAr = p.NameAr,
            NameEn = p.NameEn,
            Description = p.Description,
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.NameAr,
            UnitId = p.UnitId,
            UnitName = p.Unit?.NameAr,
            PurchasePrice = p.PurchasePrice,
            SalePrice = p.SalePrice,
            MinSalePrice = p.MinSalePrice,
            VatRate = p.VatRate,
            ItemCode = p.ItemCode,
            GS1Code = p.GS1Code,
            MinStockLevel = p.MinStockLevel,
            MaxStockLevel = p.MaxStockLevel,
            TrackStock = p.TrackStock,
            IsActive = p.IsActive,
            ImageUrl = p.ImageUrl,
            CurrentStock = currentStock
        };
    }
}
