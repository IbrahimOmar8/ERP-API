using Domain.Models.Egypt;

namespace Application.Inerfaces.Egypt
{
    public interface IEInvoiceService
    {
        // Builds, signs (placeholder) and submits a sale to the Egyptian Tax Authority (ETA).
        // signedCmsBase64 lets the caller supply a real PKCS#7 signature from an external signer
        // (USB token / HSM). When null, a hash-only payload is sent (useful in preprod sandbox).
        Task<EInvoiceSubmission> SubmitSaleAsync(Guid saleId, string? signedCmsBase64 = null, CancellationToken ct = default);

        // Refreshes status of an already-submitted document by querying ETA by UUID.
        Task<EInvoiceSubmission> RefreshStatusAsync(Guid saleId, CancellationToken ct = default);

        // Cancels (rejects) a submitted document at ETA. Reason is mandatory.
        Task<EInvoiceSubmission> CancelAsync(Guid saleId, string reason, CancellationToken ct = default);

        Task<EInvoiceSubmission?> GetSubmissionAsync(Guid saleId);

        Task<IReadOnlyList<EInvoiceSubmission>> GetRecentAsync(int take = 50);
    }
}
