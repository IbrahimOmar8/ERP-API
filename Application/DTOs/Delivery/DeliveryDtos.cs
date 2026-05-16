using System.ComponentModel.DataAnnotations;
using Domain.Enums;

namespace Application.DTOs.Delivery
{
    // ─── Drivers ────────────────────────────────────────────────────────

    public class DriverDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? NationalId { get; set; }
        public DriverVehicleType VehicleType { get; set; }
        public string? VehicleNumber { get; set; }
        public decimal CommissionPerDelivery { get; set; }
        public bool IsActive { get; set; }
        public string? Notes { get; set; }

        // Live counters
        public int ActiveOrders { get; set; }
        public decimal CashHeld { get; set; } // collected but not yet settled (Delivered minus settled — for now we treat each Delivered as immediately settled and show 0)
    }

    public class CreateDriverDto
    {
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        [StringLength(50)] public string? Phone { get; set; }
        [StringLength(20)] public string? NationalId { get; set; }
        public DriverVehicleType VehicleType { get; set; } = DriverVehicleType.Motorcycle;
        [StringLength(50)] public string? VehicleNumber { get; set; }
        public decimal CommissionPerDelivery { get; set; }
        public bool IsActive { get; set; } = true;
        [StringLength(500)] public string? Notes { get; set; }
    }

    // ─── Zones ──────────────────────────────────────────────────────────

    public class DeliveryZoneDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public int EstimatedMinutes { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateZoneDto
    {
        [Required, StringLength(150)] public string Name { get; set; } = string.Empty;
        public decimal Fee { get; set; }
        public int EstimatedMinutes { get; set; } = 30;
        public bool IsActive { get; set; } = true;
    }

    // ─── Orders ─────────────────────────────────────────────────────────

    public class DeliveryOrderDto
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid? SaleId { get; set; }
        public string? SaleNumber { get; set; }
        public Guid? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public string Address { get; set; } = string.Empty;
        public Guid? ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal CashToCollect { get; set; }
        public decimal CashCollected { get; set; }
        public Guid? DriverId { get; set; }
        public string? DriverName { get; set; }
        public DeliveryStatus Status { get; set; }
        public DateTime? AssignedAt { get; set; }
        public DateTime? PickedUpAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDeliveryOrderDto
    {
        public Guid? SaleId { get; set; }
        public Guid? CustomerId { get; set; }
        [StringLength(150)] public string? CustomerName { get; set; }
        [StringLength(50)] public string? CustomerPhone { get; set; }
        [Required, StringLength(500)] public string Address { get; set; } = string.Empty;
        public Guid? ZoneId { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal CashToCollect { get; set; }
        public Guid? DriverId { get; set; }
        [StringLength(500)] public string? Notes { get; set; }
    }

    public class AssignDriverDto
    {
        [Required] public Guid DriverId { get; set; }
    }

    public class DeliverDto
    {
        // Amount actually handed back by the driver (default = expected)
        public decimal? CashCollected { get; set; }
    }

    public class DeliveryFilterDto
    {
        public DeliveryStatus? Status { get; set; }
        public Guid? DriverId { get; set; }
        public Guid? ZoneId { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }

    public class DriverReconciliationRow
    {
        public Guid DriverId { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public int DeliveredCount { get; set; }
        public decimal CashCollected { get; set; }
        public decimal Commission { get; set; }
    }
}
