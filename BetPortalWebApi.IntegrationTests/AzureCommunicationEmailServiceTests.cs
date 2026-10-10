using BetPortalWebApi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public sealed class AzureCommunicationEmailServiceTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("YOUR_CONNECTION_STRING", "sender@example.test")]
    [InlineData("endpoint=https://example.test/;accesskey=YWJj", "YOUR_SENDER_EMAIL")]
    [InlineData("not-a-connection-string", "sender@example.test")]
    public async Task Missing_placeholder_or_invalid_configuration_logs_and_does_not_send(string? connectionString, string? sender)
    {
        var logger = new RecordingLogger();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AzureCommunication:ConnectionString"] = connectionString,
            ["AzureCommunication:SenderAddress"] = sender,
        }).Build();
        var service = new AzureCommunicationEmailService(configuration, logger);

        Assert.False(await service.SendEmailAsync(
            "recipient@example.test", "Subject", "<p>Body</p>", "Body", "bcc@example.test", TestContext.Current.CancellationToken));
        Assert.Contains(logger.Entries, entry => entry.Level is LogLevel.Warning or LogLevel.Error);
        Assert.Contains(logger.Entries, entry => entry.Message.Contains("disabled", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("recipient@example.test", StringComparison.Ordinal));
        if (connectionString is not null)
        {
            Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(connectionString, StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task Cancellation_is_not_reported_as_a_delivery_failure()
    {
        var service = new AzureCommunicationEmailService(new ConfigurationBuilder().Build(), new RecordingLogger());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.SendEmailAsync("recipient@example.test", "Subject", "<p>Body</p>", cancellationToken: cancellation.Token));
    }

    private sealed class RecordingLogger : ILogger<AzureCommunicationEmailService>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}
