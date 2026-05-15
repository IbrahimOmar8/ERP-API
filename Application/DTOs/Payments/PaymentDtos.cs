using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Payments
{
    public class CustomerPaymentDto
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class CreateCustomerPaymentDto
    {
        [Required] public Guid CustomerId { get; set; }
        [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        [StringLength(100)] public string? Reference { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    public class SupplierPaymentDto
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public DateTime PaymentDate { get; set; }
    }

    public class CreateSupplierPaymentDto
    {
        [Required] public Guid SupplierId { get; set; }
        [Range(0.01, double.MaxValue)] public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
        [StringLength(100)] public string? Reference { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
        public DateTime? PaymentDate { get; set; }
    }

    // Combined ledger row — either a sale/purchase (debit) or a payment (credit)
    public class LedgerRow
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty; // "sale", "payment", "purchase", "refund"
        public string Reference { get; set; } = string.Empty;
        public decimal Debit { get; set; }  // owed to us / us owed
        public decimal Credit { get; set; }
        public decimal Balance { get; set; } // running
        public string? Notes { get; set; }
    }

    public class CustomerLedgerDto
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public List<LedgerRow> Rows { get; set; } = new();
    }

    public class SupplierLedgerDto
    {
        public Guid SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public decimal OpeningBalance { get; set; }
        public decimal ClosingBalance { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public List<LedgerRow> Rows { get; set; } = new();
    }
}
