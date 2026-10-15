using BetPortalWebApi.Services.Interfaces;

internal sealed class RecordingEmailService(bool succeeds = true) : IEmailService
{
    public List<Message> Messages { get; } = [];

    public Func<Task>? OnSend { get; init; }

    public async Task<bool> SendEmailAsync(
        string toEmail,
        string subject,
        string htmlContent,
        string? plainTextContent = null,
        string? bcc = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Messages.Add(new Message(toEmail, subject, htmlContent, plainTextContent, bcc, cancellationToken));
        if (OnSend is not null)
        {
            await OnSend();
        }

        return succeeds;
    }

    internal sealed record Message(
        string Recipient, string Subject, string Html, string? PlainText, string? Bcc, CancellationToken CancellationToken);
}
