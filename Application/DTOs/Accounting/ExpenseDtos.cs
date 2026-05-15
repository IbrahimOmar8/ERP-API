using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Accounting
{
    public class ExpenseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public ExpenseCategory Category { get; set; }
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public Guid? CashSessionId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateExpenseDto
    {
        [Required, StringLength(150)]
        public string Title { get; set; } = string.Empty;

        public ExpenseCategory Category { get; set; } = ExpenseCategory.Other;

        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public DateTime? ExpenseDate { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

        [StringLength(100)]
        public string? Reference { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public Guid? CashSessionId { get; set; }
    }

    public class ExpenseFilterDto
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public ExpenseCategory? Category { get; set; }
        public Guid? CashSessionId { get; set; }
    }

    public class ExpenseSummaryDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal Total { get; set; }
        public int Count { get; set; }
        public List<ExpensesByCategoryRow> ByCategory { get; set; } = new();
    }

    public class ExpensesByCategoryRow
    {
        public ExpenseCategory Category { get; set; }
        public decimal Total { get; set; }
        public int Count { get; set; }
    }
}
