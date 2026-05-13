using System.Text.Json.Serialization;

namespace Application.DTOs.Egypt
{
    // Strongly typed shape of the ETA invoice JSON document.
    // Field names use camelCase to match the ETA Invoicing API schema.
    public class EtaDocument
    {
        public EtaParty Issuer { get; set; } = new();
        public EtaParty Receiver { get; set; } = new();

        public string DocumentType { get; set; } = "I";
        public string DocumentTypeVersion { get; set; } = "1.0";

        public string DateTimeIssued { get; set; } = string.Empty;
        public string TaxpayerActivityCode { get; set; } = string.Empty;
        public string InternalID { get; set; } = string.Empty;

        public List<EtaInvoiceLine> InvoiceLines { get; set; } = new();

        public decimal TotalDiscountAmount { get; set; }
        public decimal TotalSalesAmount { get; set; }
        public decimal NetAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ExtraDiscountAmount { get; set; }
        public decimal TotalItemsDiscountAmount { get; set; }

        public List<EtaTaxTotal> TaxTotals { get; set; } = new();

        public List<EtaSignature> Signatures { get; set; } = new();

        [JsonIgnore]
        public string? PurchaseOrderReference { get; set; }
    }

    public class EtaParty
    {
        public string Type { get; set; } = "B"; // B=Business, P=Person, F=Foreigner
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public EtaAddress Address { get; set; } = new();
    }

    public class EtaAddress
    {
        public string Country { get; set; } = "EG";
        public string Governate { get; set; } = string.Empty;
        public string RegionCity { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string BuildingNumber { get; set; } = "1";
        public string? PostalCode { get; set; }
        public string? Floor { get; set; }
        public string? Room { get; set; }
        public string? Landmark { get; set; }
        public string? AdditionalInformation { get; set; }
    }

    public class EtaInvoiceLine
    {
        public string Description { get; set; } = string.Empty;
        public string ItemType { get; set; } = "EGS";   // EGS = Egyptian goods/services
        public string ItemCode { get; set; } = string.Empty;
        public string UnitType { get; set; } = "EA";
        public decimal Quantity { get; set; }
        public EtaAmount UnitValue { get; set; } = new();
        public decimal SalesTotal { get; set; }
        public decimal Total { get; set; }
        public decimal ValueDifference { get; set; }
        public decimal TotalTaxableFees { get; set; }
        public decimal NetTotal { get; set; }
        public decimal ItemsDiscount { get; set; }
        public EtaDiscount Discount { get; set; } = new();
        public List<EtaTaxableItem> TaxableItems { get; set; } = new();
        public string? InternalCode { get; set; }
    }

    public class EtaAmount
    {
        public string CurrencySold { get; set; } = "EGP";
        public decimal AmountEGP { get; set; }
        public decimal? AmountSold { get; set; }
        public decimal? CurrencyExchangeRate { get; set; }
    }

    public class EtaDiscount
    {
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }

    public class EtaTaxableItem
    {
        public string TaxType { get; set; } = "T1";  // T1 = Value-added tax
        public decimal Amount { get; set; }
        public string SubType { get; set; } = "V009"; // V009 = standard 14%
        public decimal Rate { get; set; }
    }

    public class EtaTaxTotal
    {
        public string TaxType { get; set; } = "T1";
        public decimal Amount { get; set; }
    }

    public class EtaSignature
    {
        public string SignatureType { get; set; } = "I"; // I = Issuer
        public string Value { get; set; } = string.Empty;
    }

    public class EtaSubmitRequest
    {
        [JsonPropertyName("documents")]
        public List<EtaDocument> Documents { get; set; } = new();
    }

    public class EtaSubmitResponse
    {
        public string? SubmissionId { get; set; }
        public List<EtaAcceptedDocument>? AcceptedDocuments { get; set; }
        public List<EtaRejectedDocument>? RejectedDocuments { get; set; }
    }

    public class EtaAcceptedDocument
    {
        public string Uuid { get; set; } = string.Empty;
        public string LongId { get; set; } = string.Empty;
        public string InternalId { get; set; } = string.Empty;
        public string HashKey { get; set; } = string.Empty;
    }

    public class EtaRejectedDocument
    {
        public string InternalId { get; set; } = string.Empty;
        public object? Error { get; set; }
    }
}
