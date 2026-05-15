namespace Domain.Enums
{
    public enum QuotationStatus
    {
        Draft = 0,       // مسودة
        Sent = 1,        // أُرسل للعميل
        Accepted = 2,    // وافق العميل
        Rejected = 3,    // رفضه العميل
        Expired = 4,     // انتهت صلاحيته
        Converted = 5,   // حُوّل لفاتورة بيع
        Cancelled = 6,   // أُلغي
    }
}
