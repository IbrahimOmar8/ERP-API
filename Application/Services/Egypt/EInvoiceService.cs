using Application.Inerfaces.Egypt;
using Domain.Enums;
using Domain.Models.Egypt;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Egypt
{
    // Stub implementation for Egyptian Tax Authority (ETA) e-invoicing
    // Production-ready integration requires ETA OAuth2, JSON document signing
    // and submission to https://api.invoicing.eta.gov.eg
    public class EInvoiceService : IEInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public EInvoiceService(ApplicationDbContext context) => _context = context;

        public async Task<EInvoiceSubmission> SubmitSaleAsync(Guid saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Items)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == saleId)
                ?? throw new InvalidOperationException("الفاتورة غير موجودة");

            var company = await _context.CompanyProfiles.FirstOrDefaultAsync();
            if (company == null || !company.EtaEnabled)
                throw new InvalidOperationException("لم يتم تفعيل تكامل الفاتورة الإلكترونية");

            var submission = new EInvoiceSubmission
            {
                SaleId = saleId,
                SubmissionUuid = Guid.NewGuid().ToString(),
                Status = EInvoiceStatus.Submitted,
                SubmittedAt = DateTime.UtcNow,
                RequestPayload = BuildDocumentJson(sale, company)
            };

            // TODO: Real ETA submission with OAuth2 token + signed JSON
            submission.Status = EInvoiceStatus.Valid;
            submission.LongId = $"ETA-{DateTime.UtcNow:yyyyMMddHHmmss}-{submission.SubmissionUuid![..8]}";
            submission.ValidatedAt = DateTime.UtcNow;

            _context.EInvoiceSubmissions.Add(submission);

            sale.EInvoiceUuid = submission.SubmissionUuid;
            sale.EInvoiceLongId = submission.LongId;
            sale.EInvoiceStatus = submission.Status;

            await _context.SaveChangesAsync();
            return submission;
        }

        public async Task<EInvoiceSubmission?> GetSubmissionAsync(Guid saleId)
        {
            return await _context.EInvoiceSubmissions
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);
        }

        private static string BuildDocumentJson(Domain.Models.POS.Sale sale, CompanyProfile company)
        {
            // Simplified ETA document - real impl uses System.Text.Json with full schema
            return System.Text.Json.JsonSerializer.Serialize(new
            {
                issuer = new
                {
                    type = "B",
                    id = company.TaxRegistrationNumber,
                    name = company.NameAr,
                    address = new { country = "EG", governate = company.Governorate, regionCity = company.City, street = company.Address }
                },
                receiver = sale.CustomerId.HasValue ? new
                {
                    type = sale.Customer!.IsCompany ? "B" : "P",
                    id = sale.Customer.TaxRegistrationNumber ?? sale.Customer.NationalId ?? "",
                    name = sale.Customer.Name
                } : null,
                documentType = "I",
                documentTypeVersion = "1.0",
                dateTimeIssued = sale.SaleDate,
                taxpayerActivityCode = company.ActivityCode,
                internalID = sale.InvoiceNumber,
                invoiceLines = sale.Items?.Select(i => new
                {
                    description = i.ProductNameSnapshot,
                    itemType = "EGS",
                    itemCode = "",
                    unitType = "EA",
                    quantity = i.Quantity,
                    unitValue = new { currencySold = "EGP", amountEGP = i.UnitPrice },
                    salesTotal = i.LineSubTotal,
                    total = i.LineTotal,
                    valueDifference = 0,
                    totalTaxableFees = 0,
                    netTotal = i.LineSubTotal,
                    itemsDiscount = 0,
                    discount = new { rate = i.DiscountPercent, amount = i.DiscountAmount },
                    taxableItems = new[] { new { taxType = "T1", amount = i.VatAmount, subType = "V009", rate = i.VatRate } }
                }),
                totalSalesAmount = sale.SubTotal,
                totalDiscountAmount = sale.DiscountAmount,
                netAmount = sale.SubTotal - sale.DiscountAmount,
                totalAmount = sale.Total,
                taxTotals = new[] { new { taxType = "T1", amount = sale.VatAmount } }
            });
        }
    }
}
