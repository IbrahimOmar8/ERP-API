using Application.Services.Egypt;
using Domain.Models.Egypt;
using Domain.Models.Inventory;
using Domain.Models.POS;

namespace Tests.Egypt;

public class EtaDocumentBuilderTests
{
    private static Sale SampleSale() => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = "INV-001",
        SaleDate = new DateTime(2025, 1, 15, 10, 30, 0, DateTimeKind.Utc),
        SubTotal = 100m,
        DiscountAmount = 10m,
        VatAmount = 12.6m,
        Total = 102.6m,
        Items = new List<SaleItem>
        {
            new()
            {
                ProductId = Guid.NewGuid(),
                ProductNameSnapshot = "صنف اختبار",
                Quantity = 2,
                UnitPrice = 50m,
                DiscountAmount = 10m,
                DiscountPercent = 10m,
                VatRate = 14m,
                VatAmount = 12.6m,
                LineSubTotal = 100m,
                LineTotal = 102.6m,
                Product = new Product { Sku = "SKU-1", Barcode = "B-1" }
            }
        }
    };

    private static CompanyProfile SampleCompany() => new()
    {
        NameAr = "متجري",
        TaxRegistrationNumber = "123456789",
        Address = "Cairo",
        ActivityCode = "4711",
        Governorate = "Cairo",
        City = "Cairo"
    };

    [Fact]
    public void Build_AssignsIssuerFromCompany()
    {
        var doc = EtaDocumentBuilder.Build(SampleSale(), SampleCompany());
        doc.Issuer.Id.Should().Be("123456789");
        doc.Issuer.Name.Should().Be("متجري");
        doc.Issuer.Type.Should().Be("B");
        doc.TaxpayerActivityCode.Should().Be("4711");
    }

    [Fact]
    public void Build_WithNoCustomer_GeneratesCashCustomerReceiver()
    {
        var doc = EtaDocumentBuilder.Build(SampleSale(), SampleCompany());
        doc.Receiver.Type.Should().Be("P");
        doc.Receiver.Id.Should().Be("0");
        doc.Receiver.Name.Should().Be("عميل نقدي");
    }

    [Fact]
    public void Build_BusinessCustomer_UsesTrnAndBusinessType()
    {
        var sale = SampleSale();
        sale.Customer = new Customer
        {
            Name = "شركة س",
            IsCompany = true,
            TaxRegistrationNumber = "987654321"
        };
        var doc = EtaDocumentBuilder.Build(sale, SampleCompany());
        doc.Receiver.Type.Should().Be("B");
        doc.Receiver.Id.Should().Be("987654321");
    }

    [Fact]
    public void Build_FormatsDateTimeAsIsoUtc()
    {
        var doc = EtaDocumentBuilder.Build(SampleSale(), SampleCompany());
        doc.DateTimeIssued.Should().Be("2025-01-15T10:30:00Z");
    }

    [Fact]
    public void Build_PopulatesInvoiceLinesAndTotals()
    {
        var doc = EtaDocumentBuilder.Build(SampleSale(), SampleCompany());
        doc.InvoiceLines.Should().HaveCount(1);

        var line = doc.InvoiceLines[0];
        line.Description.Should().Be("صنف اختبار");
        line.ItemCode.Should().Be("B-1"); // barcode preferred over sku
        line.Quantity.Should().Be(2m);
        line.UnitValue.AmountEGP.Should().Be(50m);
        line.TaxableItems.Should().ContainSingle(t => t.TaxType == "T1" && t.SubType == "V009");

        doc.TotalAmount.Should().Be(102.6m);
        doc.TaxTotals.Should().ContainSingle(t => t.Amount == 12.6m);
    }
}
