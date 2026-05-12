using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.POS
{
    public class SalePayment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SaleId { get; set; }
        public Sale? Sale { get; set; }

        public PaymentMethod Method { get; set; }
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string? Reference { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    }
}
