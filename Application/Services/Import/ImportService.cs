using System.Globalization;
using Application.DTOs.Import;
using Application.Inerfaces.Import;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.Models.Inventory;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Import
{
    public class ImportService : IImportService
    {
        private readonly ApplicationDbContext _context;
        public ImportService(ApplicationDbContext context) => _context = context;

        public async Task<ImportResultDto> ImportProductsAsync(Stream csv, bool dryRun, CancellationToken ct = default)
        {
            var result = new ImportResultDto { DryRun = dryRun };
            var rows = ParseCsv(csv, result);
            if (rows.Count == 0) return result;

            // Lookups
            var existingProducts = await _context.Products.ToDictionaryAsync(p => p.Sku, ct);
            var categoriesByName = await _context.Categories.ToDictionaryAsync(c => c.NameAr, c => c, StringComparer.OrdinalIgnoreCase, ct);
            var unitsByName = await _context.Units.ToDictionaryAsync(u => u.NameAr, u => u, StringComparer.OrdinalIgnoreCase, ct);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var lineNo = i + 2; // header is row 1
                result.Total++;

                try
                {
                    var sku = Get(row, "sku").Trim();
                    var nameAr = Get(row, "nameAr").Trim();
                    if (string.IsNullOrEmpty(sku))
                    {
                        result.Errors.Add(new ImportRowError { Row = lineNo, Field = "sku", Message = "SKU مطلوب" });
                        result.Skipped++;
                        continue;
                    }
                    if (string.IsNullOrEmpty(nameAr))
                    {
                        result.Errors.Add(new ImportRowError { Row = lineNo, Field = "nameAr", Message = "اسم الصنف مطلوب" });
                        result.Skipped++;
                        continue;
                    }

                    var categoryName = Get(row, "category").Trim();
                    var unitName = Get(row, "unit").Trim();
                    if (string.IsNullOrEmpty(categoryName)) categoryName = "عام";
                    if (string.IsNullOrEmpty(unitName)) unitName = "قطعة";

                    var category = ResolveCategory(categoriesByName, categoryName, dryRun);
                    var unit = ResolveUnit(unitsByName, unitName, dryRun);

                    if (existingProducts.TryGetValue(sku, out var product))
                    {
                        product.NameAr = nameAr;
                        product.NameEn = Get(row, "nameEn");
                        product.Barcode = Get(row, "barcode");
                        product.CategoryId = category.Id;
                        product.UnitId = unit.Id;
                        product.PurchasePrice = ParseDecimal(Get(row, "purchasePrice"));
                        product.SalePrice = ParseDecimal(Get(row, "salePrice"));
                        product.VatRate = ParseDecimal(Get(row, "vatRate"), defaultValue: 14m);
                        product.MinStockLevel = ParseDecimal(Get(row, "minStockLevel"));
                        product.UpdatedAt = DateTime.UtcNow;
                        result.Updated++;
                    }
                    else
                    {
                        var p = new Product
                        {
                            Sku = sku,
                            NameAr = nameAr,
                            NameEn = Get(row, "nameEn"),
                            Barcode = Get(row, "barcode"),
                            CategoryId = category.Id,
                            UnitId = unit.Id,
                            PurchasePrice = ParseDecimal(Get(row, "purchasePrice")),
                            SalePrice = ParseDecimal(Get(row, "salePrice")),
                            VatRate = ParseDecimal(Get(row, "vatRate"), defaultValue: 14m),
                            MinStockLevel = ParseDecimal(Get(row, "minStockLevel")),
                        };
                        if (!dryRun) _context.Products.Add(p);
                        existingProducts[sku] = p;
                        result.Created++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportRowError { Row = lineNo, Message = ex.Message });
                    result.Skipped++;
                }
            }

            if (!dryRun) await _context.SaveChangesAsync(ct);
            return result;
        }

        public async Task<ImportResultDto> ImportCustomersAsync(Stream csv, bool dryRun, CancellationToken ct = default)
        {
            var result = new ImportResultDto { DryRun = dryRun };
            var rows = ParseCsv(csv, result);
            if (rows.Count == 0) return result;

            var byPhone = await _context.Customers
                .Where(c => c.Phone != null && c.Phone != "")
                .ToDictionaryAsync(c => c.Phone!, c => c, ct);

            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                var lineNo = i + 2;
                result.Total++;

                try
                {
                    var name = Get(row, "name").Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        result.Errors.Add(new ImportRowError { Row = lineNo, Field = "name", Message = "اسم العميل مطلوب" });
                        result.Skipped++;
                        continue;
                    }

                    var phone = Get(row, "phone").Trim();
                    var existing = !string.IsNullOrEmpty(phone) && byPhone.TryGetValue(phone, out var c) ? c : null;

                    if (existing != null)
                    {
                        existing.Name = name;
                        existing.Email = NullIfEmpty(Get(row, "email"));
                        existing.Address = NullIfEmpty(Get(row, "address"));
                        existing.TaxRegistrationNumber = NullIfEmpty(Get(row, "taxRegistrationNumber"));
                        existing.NationalId = NullIfEmpty(Get(row, "nationalId"));
                        existing.IsCompany = ParseBool(Get(row, "isCompany"));
                        existing.CreditLimit = ParseDecimal(Get(row, "creditLimit"));
                        result.Updated++;
                    }
                    else
                    {
                        var newCustomer = new Customer
                        {
                            Name = name,
                            Phone = NullIfEmpty(phone),
                            Email = NullIfEmpty(Get(row, "email")),
                            Address = NullIfEmpty(Get(row, "address")),
                            TaxRegistrationNumber = NullIfEmpty(Get(row, "taxRegistrationNumber")),
                            NationalId = NullIfEmpty(Get(row, "nationalId")),
                            IsCompany = ParseBool(Get(row, "isCompany")),
                            CreditLimit = ParseDecimal(Get(row, "creditLimit")),
                        };
                        if (!dryRun) _context.Customers.Add(newCustomer);
                        if (!string.IsNullOrEmpty(phone)) byPhone[phone] = newCustomer;
                        result.Created++;
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ImportRowError { Row = lineNo, Message = ex.Message });
                    result.Skipped++;
                }
            }

            if (!dryRun) await _context.SaveChangesAsync(ct);
            return result;
        }

        // ─── helpers ──────────────────────────────────────────────────────

        private static List<Dictionary<string, string>> ParseCsv(Stream csv, ImportResultDto result)
        {
            var rows = new List<Dictionary<string, string>>();
            try
            {
                using var reader = new StreamReader(csv);
                var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
                    BadDataFound = null,
                };
                using var csvReader = new CsvReader(reader, cfg);
                csvReader.Read();
                csvReader.ReadHeader();
                var headers = csvReader.HeaderRecord ?? Array.Empty<string>();
                while (csvReader.Read())
                {
                    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var h in headers)
                    {
                        try { dict[h] = csvReader.GetField(h) ?? ""; }
                        catch { dict[h] = ""; }
                    }
                    rows.Add(dict);
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(new ImportRowError { Row = 0, Message = $"تعذر قراءة الملف: {ex.Message}" });
            }
            return rows;
        }

        private Category ResolveCategory(Dictionary<string, Category> map, string name, bool dryRun)
        {
            if (map.TryGetValue(name, out var c)) return c;
            var created = new Category { NameAr = name };
            if (!dryRun) _context.Categories.Add(created);
            map[name] = created;
            return created;
        }

        private Unit ResolveUnit(Dictionary<string, Unit> map, string name, bool dryRun)
        {
            if (map.TryGetValue(name, out var u)) return u;
            var code = name.Length > 10 ? name.Substring(0, 10) : name;
            var created = new Unit { NameAr = name, Code = code };
            if (!dryRun) _context.Units.Add(created);
            map[name] = created;
            return created;
        }

        private static string Get(Dictionary<string, string> row, string key) =>
            row.TryGetValue(key, out var v) ? v ?? "" : "";

        private static string? NullIfEmpty(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static decimal ParseDecimal(string s, decimal defaultValue = 0m) =>
            decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d)
                ? d
                : decimal.TryParse(s, NumberStyles.Any, CultureInfo.GetCultureInfo("ar-EG"), out var ar)
                    ? ar : defaultValue;

        private static bool ParseBool(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            var v = s.Trim().ToLowerInvariant();
            return v is "true" or "1" or "yes" or "y" or "نعم" or "شركة";
        }
    }
}
