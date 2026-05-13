using Application.DTOs.Egypt;
using Domain.Models.Egypt;
using Domain.Models.POS;

namespace Application.Services.Egypt
{
    public static class EtaDocumentBuilder
    {
        public static EtaDocument Build(Sale sale, CompanyProfile company)
        {
            var doc = new EtaDocument
            {
                Issuer = new EtaParty
                {
                    Type = "B",
                    Id = company.TaxRegistrationNumber,
                    Name = company.NameAr,
                    Address = new EtaAddress
                    {
                        Country = "EG",
                        Governate = company.Governorate ?? "Cairo",
                        RegionCity = company.City ?? "Cairo",
                        Street = company.Address,
                        BuildingNumber = "1"
                    }
                },
                Receiver = BuildReceiver(sale.Customer),
                DocumentType = "I",
                DocumentTypeVersion = "1.0",
                DateTimeIssued = sale.SaleDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
                TaxpayerActivityCode = company.ActivityCode ?? "4711",
                InternalID = sale.InvoiceNumber,
                TotalDiscountAmount = Round(sale.DiscountAmount),
                TotalSalesAmount = Round(sale.SubTotal),
                NetAmount = Round(sale.SubTotal - sale.DiscountAmount),
                TotalAmount = Round(sale.Total),
                ExtraDiscountAmount = 0,
                TotalItemsDiscountAmount = 0,
                TaxTotals = new List<EtaTaxTotal>
                {
                    new() { TaxType = "T1", Amount = Round(sale.VatAmount) }
                }
            };

            if (sale.Items != null)
            {
                foreach (var i in sale.Items)
                {
                    doc.InvoiceLines.Add(new EtaInvoiceLine
                    {
                        Description = i.ProductNameSnapshot,
                        ItemType = "EGS",
                        ItemCode = i.Product?.Barcode ?? i.Product?.Sku ?? "EG-NA",
                        UnitType = "EA",
                        Quantity = Round(i.Quantity),
                        UnitValue = new EtaAmount
                        {
                            CurrencySold = "EGP",
                            AmountEGP = Round(i.UnitPrice)
                        },
                        SalesTotal = Round(i.LineSubTotal),
                        Total = Round(i.LineTotal),
                        ValueDifference = 0,
                        TotalTaxableFees = 0,
                        NetTotal = Round(i.LineSubTotal - i.DiscountAmount),
                        ItemsDiscount = 0,
                        Discount = new EtaDiscount
                        {
                            Rate = Round(i.DiscountPercent),
                            Amount = Round(i.DiscountAmount)
                        },
                        TaxableItems = new List<EtaTaxableItem>
                        {
                            new()
                            {
                                TaxType = "T1",
                                SubType = "V009",
                                Amount = Round(i.VatAmount),
                                Rate = Round(i.VatRate)
                            }
                        }
                    });
                }
            }

            return doc;
        }

        private static EtaParty BuildReceiver(Domain.Models.POS.Customer? c)
        {
            if (c == null)
                return new EtaParty
                {
                    Type = "P",
                    Id = "0",
                    Name = "عميل نقدي",
                    Address = new EtaAddress { Country = "EG", Governate = "Cairo", RegionCity = "Cairo", Street = "N/A" }
                };

            return new EtaParty
            {
                Type = c.IsCompany ? "B" : "P",
                Id = c.TaxRegistrationNumber ?? c.NationalId ?? "0",
                Name = c.Name,
                Address = new EtaAddress
                {
                    Country = "EG",
                    Governate = "Cairo",
                    RegionCity = "Cairo",
                    Street = c.Address ?? "N/A"
                }
            };
        }

        private static decimal Round(decimal v) => Math.Round(v, 5, MidpointRounding.AwayFromZero);
    }
}
