using Application.DTOs.Inventory;
using Application.Inerfaces.Inventory;
using Domain.Enums;
using Domain.Models.Inventory;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Inventory
{
    public class PurchaseInvoiceService : IPurchaseInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IStockService _stockService;

        public PurchaseInvoiceService(ApplicationDbContext context, IStockService stockService)
        {
            _context = context;
            _stockService = stockService;
        }

        public async Task<List<PurchaseInvoiceDto>> GetAllAsync()
        {
            return await _context.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.Warehouse)
                .Include(p => p.Items)!
                    .ThenInclude(i => i.Product)
                .OrderByDescending(p => p.InvoiceDate)
                .Select(p => MapInvoice(p))
                .ToListAsync();
        }

        public async Task<PurchaseInvoiceDto?> GetByIdAsync(Guid id)
        {
            var invoice = await _context.PurchaseInvoices
                .Include(p => p.Supplier)
                .Include(p => p.Warehouse)
                .Include(p => p.Items)!
                    .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == id);

            return invoice == null ? null : MapInvoice(invoice);
        }

        public async Task<PurchaseInvoiceDto> CreateAsync(CreatePurchaseInvoiceDto dto, Guid? userId)
        {
            var invoiceNumber = await GenerateInvoiceNumberAsync();

            var invoice = new PurchaseInvoice
            {
                InvoiceNumber = invoiceNumber,
                SupplierId = dto.SupplierId,
                WarehouseId = dto.WarehouseId,
                InvoiceDate = dto.InvoiceDate ?? DateTime.UtcNow,
                Notes = dto.Notes,
                Paid = dto.Paid,
                CreatedByUserId = userId,
                Items = new List<PurchaseInvoiceItem>()
            };

            decimal subTotal = 0, totalVat = 0, totalDiscount = 0;

            foreach (var itemDto in dto.Items)
            {
                var lineSubBeforeDiscount = itemDto.Quantity * itemDto.UnitCost;
                var lineSub = lineSubBeforeDiscount - itemDto.DiscountAmount;
                var vatAmount = lineSub * (itemDto.VatRate / 100m);
                var lineTotal = lineSub + vatAmount;

                invoice.Items.Add(new PurchaseInvoiceItem
                {
                    ProductId = itemDto.ProductId,
                    Quantity = itemDto.Quantity,
                    UnitCost = itemDto.UnitCost,
                    DiscountAmount = itemDto.DiscountAmount,
                    VatRate = itemDto.VatRate,
                    VatAmount = vatAmount,
                    LineTotal = lineTotal
                });

                subTotal += lineSubBeforeDiscount;
                totalDiscount += itemDto.DiscountAmount;
                totalVat += vatAmount;
            }

            invoice.SubTotal = subTotal;
            invoice.DiscountAmount = totalDiscount;
            invoice.VatAmount = totalVat;
            invoice.Total = subTotal - totalDiscount + totalVat;

            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.PurchaseInvoices.Add(invoice);
                await _context.SaveChangesAsync();

                foreach (var item in invoice.Items)
                {
                    await _stockService.ApplyMovementAsync(item.ProductId, invoice.WarehouseId,
                        MovementType.PurchaseIn, item.Quantity, item.UnitCost,
                        invoice.Id, "PurchaseInvoice", invoice.InvoiceNumber, userId);
                }

                var supplier = await _context.Suppliers.FindAsync(invoice.SupplierId);
                if (supplier != null)
                {
                    supplier.Balance += invoice.Total - invoice.Paid;
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }

            return (await GetByIdAsync(invoice.Id))!;
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var year = DateTime.UtcNow.Year;
            var count = await _context.PurchaseInvoices
                .CountAsync(i => i.InvoiceDate.Year == year);
            return $"PI-{year}-{(count + 1):D6}";
        }

        private static PurchaseInvoiceDto MapInvoice(PurchaseInvoice p) => new()
        {
            Id = p.Id,
            InvoiceNumber = p.InvoiceNumber,
            SupplierId = p.SupplierId,
            SupplierName = p.Supplier?.Name,
            WarehouseId = p.WarehouseId,
            WarehouseName = p.Warehouse?.NameAr,
            InvoiceDate = p.InvoiceDate,
            SubTotal = p.SubTotal,
            DiscountAmount = p.DiscountAmount,
            VatAmount = p.VatAmount,
            Total = p.Total,
            Paid = p.Paid,
            Remaining = p.Remaining,
            Notes = p.Notes,
            Items = p.Items?.Select(i => new PurchaseInvoiceItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.NameAr,
                Quantity = i.Quantity,
                UnitCost = i.UnitCost,
                DiscountAmount = i.DiscountAmount,
                VatRate = i.VatRate,
                VatAmount = i.VatAmount,
                LineTotal = i.LineTotal
            }).ToList() ?? new()
        };
    }
}
