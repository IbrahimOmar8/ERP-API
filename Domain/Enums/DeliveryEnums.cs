namespace Domain.Enums
{
    public enum DriverVehicleType
    {
        Motorcycle = 0,
        Car = 1,
        Bicycle = 2,
        Walking = 3,
        Other = 4
    }

    public enum DeliveryStatus
    {
        Pending = 0,     // created, no driver assigned yet
        Assigned = 1,    // driver assigned, awaiting pick-up
        PickedUp = 2,    // driver collected the order from the shop
        Delivered = 3,   // successfully handed to the customer
        Cancelled = 4,
        Returned = 5     // customer refused the order
    }
}
