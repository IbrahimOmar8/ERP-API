using Application.DTOs.Import;

namespace Application.Inerfaces.Import
{
    public interface IImportService
    {
        // Import products from CSV. Headers expected (any order):
        //   sku, barcode, nameAr, nameEn, category, unit, purchasePrice,
        //   salePrice, vatRate, minStockLevel
        // SKU is the upsert key. Missing categories/units are auto-created.
        Task<ImportResultDto> ImportProductsAsync(Stream csv, bool dryRun, CancellationToken ct = default);

        // Import customers from CSV. Headers expected:
        //   name, phone, email, address, taxRegistrationNumber,
        //   nationalId, isCompany, creditLimit
        // Phone is the upsert key when present, else falls back to name match.
        Task<ImportResultDto> ImportCustomersAsync(Stream csv, bool dryRun, CancellationToken ct = default);
    }
}
