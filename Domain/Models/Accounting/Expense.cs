using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Accounting
{
    public class Expense
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;

        public decimal Amount { get; set; }

        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [StringLength(100)]
        public string? Reference { get; set; }   // رقم الإيصال / المستند

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CashSessionId { get; set; }
        public Guid? RecordedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
