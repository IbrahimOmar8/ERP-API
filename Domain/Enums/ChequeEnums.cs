namespace Domain.Enums
{
    public enum ChequeType
    {
        Incoming = 1,   // received from a customer
        Outgoing = 2    // issued to a supplier
    }

    public enum ChequeStatus
    {
        Pending = 0,    // received/issued but not yet acted on
        Deposited = 1,  // submitted to bank for collection
        Cleared = 2,    // bank cleared the cheque
        Bounced = 3,    // returned by the bank
        Returned = 4,   // returned to the issuer without depositing
        Cancelled = 5
    }
}
