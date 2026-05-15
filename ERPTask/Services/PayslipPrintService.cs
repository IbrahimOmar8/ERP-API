using Domain.Models.Egypt;
using Domain.Models.HR;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ERPTask.Services
{
    // Renders printable HTML payslip for a Payroll record.
    public class PayslipPrintService
    {
        private static readonly string[] MonthNamesAr =
        {
            "", "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
            "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر"
        };

        private readonly ApplicationDbContext _context;

        public PayslipPrintService(ApplicationDbContext context) => _context = context;

        public async Task<string?> RenderAsync(Guid payrollId)
        {
            var payroll = await _context.Payrolls.FirstOrDefaultAsync(p => p.Id == payrollId);
            if (payroll == null) return null;

            var employee = await _context.Employees.FirstOrDefaultAsync(e => e.Id == payroll.EmployeeId);
            var company = await _context.CompanyProfiles.FirstOrDefaultAsync()
                          ?? new CompanyProfile { NameAr = "الشركة" };

            return Render(payroll, employee?.Name, employee?.NationalId, employee?.BankName, employee?.BankAccount, company);
        }

        private static string Render(Payroll p, string? empName, string? nationalId, string? bank, string? account, CompanyProfile c)
        {
            var monthLabel = $"{MonthNamesAr[p.Month]} {p.Year}";
            var totalDeductions = p.Deductions + p.LatePenalty + p.UnpaidLeavePenalty + p.Tax + p.InsuranceContribution;

            return $@"<!doctype html>
<html dir='rtl' lang='ar'>
<head>
<meta charset='utf-8'>
<title>قسيمة راتب — {Encode(empName ?? "—")} — {monthLabel}</title>
<style>
  body {{ font-family: 'Segoe UI', Tahoma, sans-serif; margin: 0; padding: 32px; color:#222; }}
  h1, h2 {{ margin: 0; }}
  .header {{ display:flex; justify-content:space-between; align-items:flex-start; border-bottom:2px solid #333; padding-bottom:12px; margin-bottom:20px; }}
  .meta {{ font-size:13px; color:#555; }}
  table {{ width:100%; border-collapse:collapse; margin-top:16px; font-size:14px; }}
  th,td {{ border:1px solid #ccc; padding:6px 10px; text-align:right; }}
  th {{ background:#f5f5f5; }}
  td.num {{ font-variant-numeric: tabular-nums; }}
  .grid2 {{ display:grid; grid-template-columns:1fr 1fr; gap:16px; }}
  .badge {{ background:#0ea5e9; color:white; padding:2px 8px; border-radius:12px; font-size:12px; }}
  .totals tr.grand td {{ border-top:2px solid #333; font-weight:bold; font-size:16px; }}
  .foot {{ margin-top:32px; font-size:12px; color:#777; text-align:center; }}
  .sign {{ margin-top:48px; display:flex; justify-content:space-between; font-size:13px; }}
  .sign div {{ width:30%; border-top:1px solid #999; padding-top:6px; text-align:center; }}
</style>
</head>
<body>
  <div class='header'>
    <div>
      <h1>{Encode(c.NameAr)}</h1>
      <div class='meta'>{Encode(c.Address ?? "")}</div>
      <div class='meta'>الرقم الضريبي: {Encode(c.TaxRegistrationNumber ?? "—")}</div>
    </div>
    <div style='text-align:left'>
      <h2>قسيمة راتب</h2>
      <div class='meta'>عن شهر <strong>{monthLabel}</strong></div>
      <div class='meta'><span class='badge'>{StatusLabel(p.Status)}</span></div>
    </div>
  </div>

  <table>
    <tr>
      <th style='width:25%'>الموظف</th><td>{Encode(empName ?? "—")}</td>
      <th style='width:25%'>الرقم القومي</th><td>{Encode(nationalId ?? "—")}</td>
    </tr>
    <tr>
      <th>أيام العمل</th><td>{p.WorkingDays}</td>
      <th>أيام الغياب</th><td>{p.AbsentDays}</td>
    </tr>
    <tr>
      <th>ساعات الإضافي</th><td>{p.OvertimeHours:F2}</td>
      <th>دقائق التأخير</th><td>{p.LateMinutes}</td>
    </tr>
    <tr>
      <th>البنك</th><td>{Encode(bank ?? "—")}</td>
      <th>رقم الحساب</th><td>{Encode(account ?? "—")}</td>
    </tr>
  </table>

  <div class='grid2' style='margin-top:24px;'>
    <table class='totals'>
      <thead><tr><th colspan='2' style='background:#dcfce7'>الإضافات</th></tr></thead>
      <tbody>
        <tr><td>الراتب الأساسي</td><td class='num'>{p.BaseSalary:F2}</td></tr>
        <tr><td>البدلات</td><td class='num'>{p.Allowances:F2}</td></tr>
        <tr><td>الساعات الإضافية</td><td class='num'>{p.OvertimePay:F2}</td></tr>
        <tr><td>المكافأة</td><td class='num'>{p.Bonus:F2}</td></tr>
        <tr class='grand'><td>إجمالي الأجر</td><td class='num'>{p.GrossPay:F2}</td></tr>
      </tbody>
    </table>

    <table class='totals'>
      <thead><tr><th colspan='2' style='background:#fee2e2'>الاستقطاعات</th></tr></thead>
      <tbody>
        <tr><td>الاستقطاعات الثابتة</td><td class='num'>{p.Deductions:F2}</td></tr>
        <tr><td>غرامة التأخير</td><td class='num'>{p.LatePenalty:F2}</td></tr>
        <tr><td>إجازات بدون أجر</td><td class='num'>{p.UnpaidLeavePenalty:F2}</td></tr>
        <tr><td>الضريبة</td><td class='num'>{p.Tax:F2}</td></tr>
        <tr><td>التأمين الاجتماعي</td><td class='num'>{p.InsuranceContribution:F2}</td></tr>
        <tr class='grand'><td>إجمالي الخصومات</td><td class='num'>{totalDeductions:F2}</td></tr>
      </tbody>
    </table>
  </div>

  <table style='margin-top:20px; background:#f0fdfa;'>
    <tr>
      <th style='width:50%; font-size:16px;'>صافي الراتب المستحق</th>
      <td class='num' style='font-size:20px; font-weight:bold;'>{p.NetPay:F2}</td>
    </tr>
  </table>

  <div class='sign'>
    <div>توقيع المحاسب</div>
    <div>توقيع المدير</div>
    <div>توقيع الموظف</div>
  </div>

  <div class='foot'>
    تم إصدار هذه القسيمة بتاريخ {DateTime.UtcNow:yyyy-MM-dd HH:mm} — هذه القسيمة سند رسمي.
  </div>
</body>
</html>";
        }

        private static string StatusLabel(Domain.Enums.PayrollStatus s) => s switch
        {
            Domain.Enums.PayrollStatus.Draft => "مسودة",
            Domain.Enums.PayrollStatus.Approved => "معتمدة",
            Domain.Enums.PayrollStatus.Paid => "مدفوعة",
            Domain.Enums.PayrollStatus.Cancelled => "ملغاة",
            _ => "—",
        };

        private static string Encode(string? s)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            return System.Net.WebUtility.HtmlEncode(s);
        }
    }
}
