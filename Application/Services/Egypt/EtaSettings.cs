namespace Application.Services.Egypt
{
    public class EtaSettings
    {
        public string BaseUrl { get; set; } = "https://api.preprod.invoicing.eta.gov.eg";
        public string AuthUrl { get; set; } = "https://id.preprod.eta.gov.eg/connect/token";
        public bool Enabled { get; set; }
        public int RequestTimeoutSeconds { get; set; } = 60;
        public string Scope { get; set; } = "InvoicingAPI";
    }
}
