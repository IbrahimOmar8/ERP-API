using Application.DTOs.Delivery;

namespace Application.Inerfaces.Delivery
{
    public interface IDriverService
    {
        Task<List<DriverDto>> GetAllAsync(bool? activeOnly, CancellationToken ct = default);
        Task<DriverDto> CreateAsync(CreateDriverDto dto, CancellationToken ct = default);
        Task<DriverDto?> UpdateAsync(Guid id, CreateDriverDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface IDeliveryZoneService
    {
        Task<List<DeliveryZoneDto>> GetAllAsync(CancellationToken ct = default);
        Task<DeliveryZoneDto> CreateAsync(CreateZoneDto dto, CancellationToken ct = default);
        Task<DeliveryZoneDto?> UpdateAsync(Guid id, CreateZoneDto dto, CancellationToken ct = default);
        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    }

    public interface IDeliveryOrderService
    {
        Task<List<DeliveryOrderDto>> GetAsync(DeliveryFilterDto filter, CancellationToken ct = default);
        Task<DeliveryOrderDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<DeliveryOrderDto> CreateAsync(CreateDeliveryOrderDto dto, Guid? userId, CancellationToken ct = default);

        Task<DeliveryOrderDto?> AssignAsync(Guid id, Guid driverId, CancellationToken ct = default);
        Task<DeliveryOrderDto?> PickUpAsync(Guid id, CancellationToken ct = default);
        Task<DeliveryOrderDto?> DeliverAsync(Guid id, DeliverDto dto, Guid? userId, CancellationToken ct = default);
        Task<DeliveryOrderDto?> CancelAsync(Guid id, CancellationToken ct = default);
        Task<DeliveryOrderDto?> ReturnAsync(Guid id, CancellationToken ct = default);

        Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

        Task<List<DriverReconciliationRow>> GetReconciliationAsync(DateTime from, DateTime to, CancellationToken ct = default);
    }
}
