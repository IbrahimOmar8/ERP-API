namespace Application.Inerfaces.Egypt
{
    public interface IEtaTokenService
    {
        Task<string> GetAccessTokenAsync(string clientId, string clientSecret, CancellationToken ct = default);
        void Invalidate(string clientId);
    }
}
