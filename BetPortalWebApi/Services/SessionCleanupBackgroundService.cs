using BetPortalWebApi.Services.Interfaces;

namespace BetPortalWebApi.Services
{
    public class SessionCleanupBackgroundService : BackgroundService
    {
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(1);
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SessionCleanupBackgroundService> _logger;

        public SessionCleanupBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SessionCleanupBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
                    await sessionService.CleanupExpiredSessionsAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Expired session cleanup failed.");
                }

                await Task.Delay(CleanupInterval, stoppingToken);
            }
        }
    }
}
