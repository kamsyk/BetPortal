using Azure;
using Azure.Communication.Email;
using BetPortalWebApi.Services.Interfaces;

namespace BetPortalWebApi.Services
{
    public sealed class AzureCommunicationEmailService : IEmailService
    {
        private readonly EmailClient? _emailClient;
        private readonly string _senderAddress;
        private readonly ILogger<AzureCommunicationEmailService> _logger;

        public AzureCommunicationEmailService(IConfiguration configuration, ILogger<AzureCommunicationEmailService> logger)
        {
            _logger = logger;
            var connectionString = configuration["AzureCommunication:ConnectionString"];
            _senderAddress = configuration["AzureCommunication:SenderAddress"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(connectionString)
                || connectionString.Contains("YOUR_CONNECTION_STRING", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(_senderAddress)
                || _senderAddress.Contains("YOUR_SENDER_EMAIL", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Azure Communication Email is not configured. Email delivery is disabled.");
                return;
            }

            try
            {
                _emailClient = new EmailClient(connectionString);
                _logger.LogInformation("Azure Communication Email initialized.");
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or FormatException)
            {
                _logger.LogError("Azure Communication Email configuration is invalid. Email delivery is disabled.");
            }
        }

        public async Task<bool> SendEmailAsync(
            string toEmail,
            string subject,
            string htmlContent,
            string? plainTextContent = null,
            string? bcc = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_emailClient == null)
            {
                _logger.LogWarning("Email could not be sent because Azure Communication Email is disabled.");
                return false;
            }

            ArgumentException.ThrowIfNullOrWhiteSpace(toEmail);
            ArgumentException.ThrowIfNullOrWhiteSpace(subject);
            ArgumentException.ThrowIfNullOrWhiteSpace(htmlContent);

            var content = new EmailContent(subject)
            {
                Html = htmlContent,
                PlainText = plainTextContent,
            };
            var recipients = new EmailRecipients(new[] { new EmailAddress(toEmail) });
            if (!string.IsNullOrWhiteSpace(bcc))
            {
                recipients.BCC.Add(new EmailAddress(bcc));
            }

            var message = new EmailMessage(_senderAddress, recipients, content);
            try
            {
                var operation = await _emailClient.SendAsync(WaitUntil.Completed, message, cancellationToken);
                if (operation.Value.Status != EmailSendStatus.Succeeded)
                {
                    _logger.LogError("Azure Communication Email send operation did not succeed. Status: {Status}", operation.Value.Status);
                    return false;
                }

                _logger.LogInformation("Azure Communication Email send operation succeeded.");
                return true;
            }
            catch (RequestFailedException exception)
            {
                _logger.LogError(
                    "Azure Communication Email request failed. Status: {Status}; error code: {ErrorCode}",
                    exception.Status,
                    exception.ErrorCode);
                return false;
            }
        }
    }
}