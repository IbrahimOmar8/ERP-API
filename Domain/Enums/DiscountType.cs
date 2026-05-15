namespace Domain.Enums
{
    public enum DiscountType
    {
        Percentage = 1,   // نسبة مئوية من إجمالي الفاتورة
        FixedAmount = 2,  // مبلغ ثابت
    }

    public enum LoyaltyTxType
    {
        Earn = 1,        // نقاط مكتسبة من فاتورة
        Redeem = 2,      // نقاط مستهلكة بفاتورة
        Adjust = 3,      // تعديل يدوي
        Expire = 4,      // انتهت صلاحية
    }
}
