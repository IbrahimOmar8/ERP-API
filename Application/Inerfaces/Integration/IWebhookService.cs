using Application.DTOs.Integration;

namespace Application.Inerfaces.Integration
{
    public interface IWebhookService
    {
        Task<List<WebhookDto>> GetAllAsync(CancellationToken ct = default);
        Task<WebhookDto> CreateAsync(CreateWebhookDto dto, Guid? userId, CancellationToken ct = default);
        Task<WebhookDto?> UpdateAsync(Guid id, CreateWebhookDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<List<WebhookDeliveryDto>> GetDeliveriesAsync(Guid subscriptionId, int take, CancellationToken ct = default);

        // Fire & forget — synchronously POSTs to each matching subscription
        // (we don't have Hangfire yet; later we'll move this to a queue).
        Task DispatchAsync(string @event, object payload, CancellationToken ct = default);
    }
}
