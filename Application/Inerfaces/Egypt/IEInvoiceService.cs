using Domain.Models.Egypt;

namespace Application.Inerfaces.Egypt
{
    public interface IEInvoiceService
    {
        // Submit a sale to the Egyptian Tax Authority (ETA)
        Task<EInvoiceSubmission> SubmitSaleAsync(Guid saleId);
        Task<EInvoiceSubmission?> GetSubmissionAsync(Guid saleId);
    }
}
