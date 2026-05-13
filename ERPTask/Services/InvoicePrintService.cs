using System.Globalization;
using System.Text;
using Domain.Models.Egypt;
using Domain.Models.POS;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace ERPTask.Services
{
    // Generates printable HTML receipts + ETA-compliant QR codes for sales invoices.
    public class InvoicePrintService
    {
        private readonly ApplicationDbContext _context;

        public InvoicePrintService(ApplicationDbContext context) => _context = context;

        public async Task<(string Html, byte[] QrPng)?> RenderAsync(Guid saleId, bool thermal80mm)
        {
            var sale = await _context.Sales
                .Include(s => s.Items!).ThenInclude(i => i.Product)
                .Include(s => s.Payments)
                .Include(s => s.Customer)
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == saleId);
            if (sale == null) return null;

            var company = await _context.CompanyProfiles.FirstOrDefaultAsync()
                          ?? new CompanyProfile { NameAr = "متجري" };

            var qr = BuildQrPayload(sale, company);
            var qrPng = GenerateQr(qr);
            var qrDataUrl = "data:image/png;base64," + Convert.ToBase64String(qrPng);

            var html = thermal80mm
                ? RenderThermal(sale, company, qrDataUrl)
                : RenderA4(sale, company, qrDataUrl);
            return (html, qrPng);
        }

        public async Task<byte[]?> RenderQrOnlyAsync(Guid saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == saleId);
            if (sale == null) return null;
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync()
                          ?? new CompanyProfile { NameAr = "متجري" };
            return GenerateQr(BuildQrPayload(sale, company));
        }

        // ETA-compatible payload: company name, TRN, invoice timestamp,
        // total, VAT, invoice number, and UUID when present.
        private static string BuildQrPayload(Sale sale, CompanyProfile company)
        {
            var sb = new StringBuilder();
            sb.Append("ETA|");
            sb.Append(company.NameAr).Append('|');
            sb.Append(company.TaxRegistrationNumber).Append('|');
            sb.Append(sale.SaleDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")).Append('|');
            sb.Append(sale.Total.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
            sb.Append(sale.VatAmount.ToString("F2", CultureInfo.InvariantCulture)).Append('|');
            sb.Append(sale.InvoiceNumber);
            if (!string.IsNullOrEmpty(sale.EInvoiceUuid))
                sb.Append('|').Append(sale.EInvoiceUuid);
            return sb.ToString();
        }

        private static byte[] GenerateQr(string payload)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
            var qr = new PngByteQRCode(data);
            return qr.GetGraphic(8);
        }

        private static string RenderA4(Sale s, CompanyProfile c, string qrDataUrl)
        {
            var items = string.Join("", (s.Items ?? Enumerable.Empty<SaleItem>()).Select(i => $@"
              <tr>
                <td>{Encode(i.ProductNameSnapshot)}</td>
                <td class='num'>{i.Quantity:F2}</td>
                <td class='num'>{i.UnitPrice:F2}</td>
                <td class='num'>{i.DiscountAmount:F2}</td>
                <td class='num'>{i.VatAmount:F2}</td>
                <td class='num'>{i.LineTotal:F2}</td>
              </tr>"));

            var payments = string.Join("", (s.Payments ?? Enumerable.Empty<SalePayment>()).Select(p =>
                $"<tr><td>{p.Method}</td><td class='num'>{p.Amount:F2}</td></tr>"));

            return $@"<!doctype html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<title>فاتورة {Encode(s.InvoiceNumber)}</title>
<style>
  body {{ font-family: 'Segoe UI', Tahoma, sans-serif; margin: 0; padding: 24px; color:#222; }}
  h1 {{ margin: 0; }}
  .header {{ display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #333; padding-bottom:12px; }}
  .company-block {{ max-width:60%; }}
  .meta {{ font-size:13px; color:#555; }}
  table {{ width:100%; border-collapse:collapse; margin-top:16px; font-size:14px; }}
  th,td {{ border:1px solid #ccc; padding:6px 8px; text-align:right; }}
  th {{ background:#f5f5f5; }}
  td.num {{ font-variant-numeric: tabular-nums; }}
  .totals {{ margin-top:16px; width:40%; margin-inline-start:auto; }}
  .totals td {{ border:none; padding:4px 8px; }}
  .totals tr.grand td {{ border-top:2px solid #333; font-weight:bold; font-size:16px; }}
  .qr {{ text-align:center; margin-top:12px; }}
  .qr img {{ width:140px; height:140px; }}
  .foot {{ margin-top:24px; font-size:12px; color:#777; text-align:center; }}
</style>
</head>
<body>
  <div class='header'>
    <div class='company-block'>
      <h1>{Encode(c.NameAr)}</h1>
      <div class='meta'>{Encode(c.Address)}</div>
      <div class='meta'>الرقم الضريبي: {Encode(c.TaxRegistrationNumber)}</div>
      <div class='meta'>{Encode(c.Phone ?? "")}</div>
    </div>
    <div class='qr'><img src='{qrDataUrl}' alt='QR'/></div>
  </div>
  <h2>فاتورة ضريبية</h2>
  <table class='meta-table'>
    <tr><td>رقم الفاتورة</td><td>{Encode(s.InvoiceNumber)}</td>
        <td>التاريخ</td><td>{s.SaleDate:yyyy-MM-dd HH:mm}</td></tr>
    <tr><td>العميل</td><td>{Encode(s.Customer?.Name ?? "عميل نقدي")}</td>
        <td>المخزن</td><td>{Encode(s.Warehouse?.NameAr ?? "")}</td></tr>
    {(string.IsNullOrEmpty(s.EInvoiceUuid) ? "" : $"<tr><td>UUID ETA</td><td colspan='3'>{Encode(s.EInvoiceUuid)}</td></tr>")}
  </table>
  <table>
    <thead>
      <tr><th>الصنف</th><th>الكمية</th><th>السعر</th><th>الخصم</th><th>الضريبة</th><th>الإجمالي</th></tr>
    </thead>
    <tbody>{items}</tbody>
  </table>
  <table class='totals'>
    <tr><td>المجموع</td><td class='num'>{s.SubTotal:F2}</td></tr>
    <tr><td>الخصم</td><td class='num'>{s.DiscountAmount:F2}</td></tr>
    <tr><td>الضريبة</td><td class='num'>{s.VatAmount:F2}</td></tr>
    <tr class='grand'><td>الإجمالي</td><td class='num'>{s.Total:F2}</td></tr>
    <tr><td>المدفوع</td><td class='num'>{s.PaidAmount:F2}</td></tr>
    <tr><td>الباقي</td><td class='num'>{s.ChangeAmount:F2}</td></tr>
  </table>
  {(string.IsNullOrEmpty(payments) ? "" : $"<table style='width:40%; margin-top:12px;'><thead><tr><th>وسيلة الدفع</th><th>المبلغ</th></tr></thead><tbody>{payments}</tbody></table>")}
  <div class='foot'>شكراً لتعاملكم معنا — {DateTime.UtcNow:yyyy-MM-dd}</div>
</body>
</html>";
        }

        private static string RenderThermal(Sale s, CompanyProfile c, string qrDataUrl)
        {
            var items = string.Join("", (s.Items ?? Enumerable.Empty<SaleItem>()).Select(i => $@"
              <div class='line'>
                <div>{Encode(i.ProductNameSnapshot)}</div>
                <div class='qty'>{i.Quantity:F2} × {i.UnitPrice:F2}</div>
                <div class='total'>{i.LineTotal:F2}</div>
              </div>
              <hr/>"));

            return $@"<!doctype html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<title>{Encode(s.InvoiceNumber)}</title>
<style>
  @page {{ size: 80mm auto; margin: 0; }}
  body {{ font-family: 'Tahoma', monospace; width: 76mm; margin: 0 auto; padding: 4mm; font-size: 12px; }}
  .center {{ text-align:center; }}
  .line {{ margin: 2px 0; }}
  .qty {{ font-size:11px; color:#555; }}
  .total {{ text-align: end; font-weight:bold; }}
  .row {{ display:flex; justify-content:space-between; }}
  hr {{ border:0; border-top:1px dashed #888; margin:2px 0; }}
  .qr {{ text-align:center; margin-top:6px; }}
  .qr img {{ width:120px; height:120px; }}
</style>
</head>
<body>
  <div class='center'><strong>{Encode(c.NameAr)}</strong></div>
  <div class='center'>{Encode(c.Address)}</div>
  <div class='center'>الرقم الضريبي: {Encode(c.TaxRegistrationNumber)}</div>
  <hr/>
  <div class='row'><span>فاتورة:</span><span>{Encode(s.InvoiceNumber)}</span></div>
  <div class='row'><span>التاريخ:</span><span>{s.SaleDate:yyyy-MM-dd HH:mm}</span></div>
  <div class='row'><span>العميل:</span><span>{Encode(s.Customer?.Name ?? "عميل نقدي")}</span></div>
  <hr/>
  {items}
  <div class='row'><span>المجموع</span><span>{s.SubTotal:F2}</span></div>
  <div class='row'><span>الخصم</span><span>{s.DiscountAmount:F2}</span></div>
  <div class='row'><span>الضريبة</span><span>{s.VatAmount:F2}</span></div>
  <hr/>
  <div class='row' style='font-size:14px;font-weight:bold;'><span>الإجمالي</span><span>{s.Total:F2}</span></div>
  <div class='row'><span>المدفوع</span><span>{s.PaidAmount:F2}</span></div>
  <div class='row'><span>الباقي</span><span>{s.ChangeAmount:F2}</span></div>
  <div class='qr'><img src='{qrDataUrl}' alt='QR'/></div>
  <div class='center' style='font-size:10px;margin-top:4px;'>شكراً لزيارتكم</div>
</body>
</html>";
        }

        private static string Encode(string? s) => System.Net.WebUtility.HtmlEncode(s ?? string.Empty);
    }
}
