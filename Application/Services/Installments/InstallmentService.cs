using Application.DTOs.Installments;
using Application.Inerfaces.Installments;
using Domain.Enums;
using Domain.Models.Installments;
using Domain.Models.Payments;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Installments
{
    public class InstallmentService : IInstallmentService
    {
        private readonly ApplicationDbContext _context;
        public InstallmentService(ApplicationDbContext context) => _context = context;

        public async Task<List<InstallmentPlanDto>> GetAllAsync(Guid? customerId, InstallmentPlanStatus? status, CancellationToken ct = default)
        {
            var q = _context.InstallmentPlans.Include(p => p.Installments).AsQueryable();
            if (customerId.HasValue) q = q.Where(p => p.CustomerId == customerId.Value);
            if (status.HasValue) q = q.Where(p => p.Status == status.Value);

            var plans = await q.OrderByDescending(p => p.StartDate).ToListAsync(ct);
            return await MapManyAsync(plans, ct);
        }

        public async Task<InstallmentPlanDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
        {
            var p = await _context.InstallmentPlans.Include(x => x.Installments).FirstOrDefaultAsync(x => x.Id == id, ct);
            if (p == null) return null;
            return (await MapManyAsync(new[] { p }, ct)).FirstOrDefault();
        }

        public async Task<InstallmentPlanDto> CreateAsync(CreateInstallmentPlanDto dto, Guid? userId, CancellationToken ct = default)
        {
            if (dto.DownPayment >= dto.TotalAmount)
                throw new InvalidOperationException("الدفعة المقدمة يجب أن تكون أقل من إجمالي المبلغ");
            if (dto.InstallmentCount < 1) dto.InstallmentCount = 1;

            var financed = dto.TotalAmount - dto.DownPayment;
            var installmentAmount = Math.Round(financed / dto.InstallmentCount, 2);
            var start = (dto.StartDate ?? DateTime.UtcNow).Date;
            var planNumber = await NextPlanNumberAsync(ct);

            var plan = new InstallmentPlan
            {
                PlanNumber = planNumber,
                CustomerId = dto.CustomerId,
                SaleId = dto.SaleId,
                TotalAmount = dto.TotalAmount,
                DownPayment = dto.DownPayment,
                FinancedAmount = financed,
                InstallmentCount = dto.InstallmentCount,
                InstallmentAmount = installmentAmount,
                Frequency = dto.Frequency,
                StartDate = start,
                Notes = dto.Notes,
                CreatedByUserId = userId,
                Status = InstallmentPlanStatus.Active,
            };

            // Build schedule. The last installment absorbs any rounding residue
            // so the sum exactly equals the financed amount.
            decimal scheduled = 0;
            for (var i = 1; i <= dto.InstallmentCount; i++)
            {
                var amount = i == dto.InstallmentCount
                    ? financed - scheduled
                    : installmentAmount;
                scheduled += amount;
                plan.Installments.Add(new Installment
                {
                    Sequence = i,
                    DueDate = AdvanceDate(start, i, dto.Frequency),
                    Amount = Math.Round(amount, 2),
                });
            }

            _context.InstallmentPlans.Add(plan);

            // If a down payment was paid up front, record a CustomerPayment for it
            if (dto.DownPayment > 0)
            {
                _context.CustomerPayments.Add(new CustomerPayment
                {
                    CustomerId = dto.CustomerId,
                    Amount = dto.DownPayment,
                    Method = PaymentMethod.Cash,
                    Reference = $"دفعة مقدمة — خطة {planNumber}",
                    PaymentDate = DateTime.UtcNow,
                    RecordedByUserId = userId,
                });
            }

            await _context.SaveChangesAsync(ct);
            return (await GetByIdAsync(plan.Id, ct))!;
        }

        public async Task<InstallmentPlanDto?> PayInstallmentAsync(Guid installmentId, PayInstallmentDto dto, Guid? userId, CancellationToken ct = default)
        {
            var inst = await _context.Installments.Include(i => i.Plan).FirstOrDefaultAsync(i => i.Id == installmentId, ct);
            if (inst == null || inst.Plan == null) return null;
            if (inst.Status == InstallmentStatus.Paid)
                throw new InvalidOperationException("القسط مسدد بالفعل");
            if (inst.Status == InstallmentStatus.Cancelled)
                throw new InvalidOperationException("القسط ملغى");

            var amount = dto.Amount ?? (inst.Amount - inst.AmountPaid);
            if (amount <= 0) throw new InvalidOperationException("المبلغ يجب أن يكون أكبر من صفر");

            var payment = new CustomerPayment
            {
                CustomerId = inst.Plan.CustomerId,
                Amount = amount,
                Method = dto.Method,
                Reference = dto.Reference ?? $"قسط {inst.Sequence} — خطة {inst.Plan.PlanNumber}",
                PaymentDate = DateTime.UtcNow,
                RecordedByUserId = userId,
            };
            _context.CustomerPayments.Add(payment);

            inst.AmountPaid += amount;
            if (inst.AmountPaid >= inst.Amount)
            {
                inst.Status = InstallmentStatus.Paid;
                inst.PaidAt = DateTime.UtcNow;
                inst.LinkedPaymentId = payment.Id;
            }

            // Check if all installments are paid → mark plan completed
            var planInstallments = await _context.Installments.Where(i => i.PlanId == inst.PlanId).ToListAsync(ct);
            var allPaid = planInstallments.All(i =>
                i.Id == inst.Id
                    ? inst.AmountPaid >= inst.Amount
                    : i.Status == InstallmentStatus.Paid || i.Status == InstallmentStatus.Cancelled);
            if (allPaid)
            {
                inst.Plan.Status = InstallmentPlanStatus.Completed;
            }

            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(inst.PlanId, ct);
        }

        public async Task<InstallmentPlanDto?> CancelAsync(Guid id, CancellationToken ct = default)
        {
            var plan = await _context.InstallmentPlans.FindAsync(new object?[] { id }, ct);
            if (plan == null) return null;
            if (plan.Status == InstallmentPlanStatus.Completed)
                throw new InvalidOperationException("لا يمكن إلغاء خطة مكتملة");

            plan.Status = InstallmentPlanStatus.Cancelled;
            await _context.Installments
                .Where(i => i.PlanId == id && i.Status == InstallmentStatus.Pending)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, InstallmentStatus.Cancelled), ct);
            await _context.SaveChangesAsync(ct);
            return await GetByIdAsync(id, ct);
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
        {
            var plan = await _context.InstallmentPlans.Include(p => p.Installments).FirstOrDefaultAsync(p => p.Id == id, ct);
            if (plan == null) return false;
            if (plan.Installments.Any(i => i.AmountPaid > 0))
                throw new InvalidOperationException("لا يمكن حذف خطة بها أقساط مسددة — استخدم الإلغاء");
            _context.InstallmentPlans.Remove(plan);
            await _context.SaveChangesAsync(ct);
            return true;
        }

        public async Task<List<InstallmentDto>> GetOverdueAsync(CancellationToken ct = default)
        {
            var today = DateTime.UtcNow.Date;
            var rows = await _context.Installments
                .Where(i => i.Status == InstallmentStatus.Pending && i.DueDate < today)
                .OrderBy(i => i.DueDate)
                .ToListAsync(ct);
            return rows.Select(i => MapInstallment(i, today)).ToList();
        }

        // ─── helpers ────────────────────────────────────────────────────

        private static DateTime AdvanceDate(DateTime start, int sequence, InstallmentFrequency frequency) => frequency switch
        {
            InstallmentFrequency.Weekly => start.AddDays(7 * sequence),
            InstallmentFrequency.BiWeekly => start.AddDays(14 * sequence),
            _ => start.AddMonths(sequence),
        };

        private async Task<string> NextPlanNumberAsync(CancellationToken ct)
        {
            var count = await _context.InstallmentPlans.CountAsync(ct);
            return $"PLAN-{(count + 1):D4}";
        }

        private static InstallmentDto MapInstallment(Installment i, DateTime today)
        {
            var status = i.Status;
            if (status == InstallmentStatus.Pending && i.DueDate < today)
                status = InstallmentStatus.Overdue;
            return new InstallmentDto
            {
                Id = i.Id, Sequence = i.Sequence,
                DueDate = i.DueDate, Amount = i.Amount, AmountPaid = i.AmountPaid,
                Status = status, PaidAt = i.PaidAt,
                DaysOverdue = i.Status == InstallmentStatus.Pending && i.DueDate < today
                    ? (int)(today - i.DueDate).TotalDays : 0,
            };
        }

        private async Task<List<InstallmentPlanDto>> MapManyAsync(IEnumerable<InstallmentPlan> plans, CancellationToken ct)
        {
            var custIds = plans.Select(p => p.CustomerId).Distinct().ToList();
            var saleIds = plans.Where(p => p.SaleId.HasValue).Select(p => p.SaleId!.Value).Distinct().ToList();
            var custs = custIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Customers.Where(x => custIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Name, ct);
            var sales = saleIds.Count == 0 ? new Dictionary<Guid, string>()
                : await _context.Sales.Where(x => saleIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.InvoiceNumber, ct);

            var today = DateTime.UtcNow.Date;
            return plans.Select(p =>
            {
                var instDtos = p.Installments.OrderBy(i => i.Sequence).Select(i => MapInstallment(i, today)).ToList();
                var totalPaid = p.DownPayment + p.Installments.Sum(i => i.AmountPaid);
                var next = p.Installments
                    .Where(i => i.Status == InstallmentStatus.Pending)
                    .OrderBy(i => i.DueDate).FirstOrDefault();

                return new InstallmentPlanDto
                {
                    Id = p.Id, PlanNumber = p.PlanNumber,
                    CustomerId = p.CustomerId,
                    CustomerName = custs.GetValueOrDefault(p.CustomerId),
                    SaleId = p.SaleId,
                    SaleNumber = p.SaleId.HasValue ? sales.GetValueOrDefault(p.SaleId.Value) : null,
                    TotalAmount = p.TotalAmount, DownPayment = p.DownPayment,
                    FinancedAmount = p.FinancedAmount,
                    InstallmentCount = p.InstallmentCount,
                    InstallmentAmount = p.InstallmentAmount,
                    Frequency = p.Frequency, StartDate = p.StartDate,
                    Status = p.Status, Notes = p.Notes,
                    TotalPaid = totalPaid,
                    Remaining = Math.Max(0, p.TotalAmount - totalPaid),
                    PaidCount = p.Installments.Count(i => i.Status == InstallmentStatus.Paid),
                    OverdueCount = instDtos.Count(i => i.Status == InstallmentStatus.Overdue),
                    NextDueDate = next?.DueDate,
                    NextDueAmount = next == null ? null : (next.Amount - next.AmountPaid),
                    Installments = instDtos,
                };
            }).ToList();
        }
    }
}
