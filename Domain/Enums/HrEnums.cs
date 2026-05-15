namespace Domain.Enums
{
    public enum AttendanceStatus
    {
        Present = 1,    // حاضر
        Absent = 2,     // غائب
        Late = 3,       // متأخر
        OnLeave = 4,    // إجازة
        Holiday = 5,    // عطلة رسمية
        Weekend = 6,    // عطلة أسبوعية
    }

    public enum LeaveType
    {
        Annual = 1,     // سنوية
        Sick = 2,       // مرضية
        Casual = 3,     // عارضة
        Unpaid = 4,     // بدون أجر
        Maternity = 5,  // ولادة
        Emergency = 6,  // طارئة
        Other = 99,
    }

    public enum LeaveStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Cancelled = 3,
    }

    public enum PayrollStatus
    {
        Draft = 0,
        Approved = 1,
        Paid = 2,
        Cancelled = 3,
    }
}
