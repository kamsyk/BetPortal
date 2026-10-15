namespace BetPortalWebApi.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlContent,
            string? plainTextContent = null,
            string? bcc = null,
            CancellationToken cancellationToken = default);
    }
}
