using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Domain.Models.Egypt
{
    // Tracks each submission attempt to the Egyptian Tax Authority (ETA)
    public class EInvoiceSubmission
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid SaleId { get; set; }

        [StringLength(100)]
        public string? SubmissionUuid { get; set; }

        [StringLength(100)]
        public string? LongId { get; set; }

        [StringLength(100)]
        public string? HashKey { get; set; }

        public EInvoiceStatus Status { get; set; } = EInvoiceStatus.Pending;

        public string? RequestPayload { get; set; }
        public string? ResponsePayload { get; set; }
        public string? ErrorMessage { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ValidatedAt { get; set; }
    }
}
