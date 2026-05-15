namespace Application.Inerfaces.Notifications
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
    }

    // Fallback used in tests or when SMTP isn't configured — just logs.
    public class NullEmailService : IEmailService
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
