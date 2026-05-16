namespace Domain.Enums
{
    public enum InstallmentFrequency
    {
        Monthly = 0,
        Weekly = 1,
        BiWeekly = 2
    }

    public enum InstallmentPlanStatus
    {
        Active = 0,        // installments still due
        Completed = 1,     // every installment paid
        Defaulted = 2,     // at least one overdue and customer abandoned
        Cancelled = 3
    }

    public enum InstallmentStatus
    {
        Pending = 0,
        Paid = 1,
        Overdue = 2,       // computed at read-time when due_date < today and not paid
        Cancelled = 3
    }
}
