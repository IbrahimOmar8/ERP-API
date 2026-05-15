using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Payments
{
    public class CustomerPayment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid CustomerId { get; set; }

        // Positive = customer paid us (reduces their balance owed)
        // Negative = refund to customer
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

        [StringLength(100)]
        public string? Reference { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public Guid? RecordedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class SupplierPayment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SupplierId { get; set; }

        // Positive = we paid the supplier (reduces what we owe them)
        public decimal Amount { get; set; }

        public PaymentMethod Method { get; set; } = PaymentMethod.Cash;

        [StringLength(100)]
        public string? Reference { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public Guid? RecordedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
