using System.Text.RegularExpressions;

namespace BetPortalWebApi.Utils
{
    public static class MicrosoftAuthenticationDiagnostics
    {
        public static string GetFailureCode(Exception? failure)
        {
            for (var current = failure; current != null; current = current.InnerException)
            {
                // Never log the raw OAuth error: it can contain credentials or callback data.
                var match = Regex.Match(
                    current.Message,
                    @"\bAADSTS[0-9]{4,10}\b",
                    RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
                    TimeSpan.FromSeconds(1));
                if (!match.Success)
                {
                    match = Regex.Match(
                        current.Message,
                        @"\b(invalid_client|invalid_grant|access_denied|unauthorized_client|invalid_scope|invalid_request|temporarily_unavailable|server_error)\b",
                        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking,
                        TimeSpan.FromSeconds(1));
                }

                if (match.Success)
                {
                    return match.Value;
                }

                if (current.Message.Contains("Correlation failed", StringComparison.OrdinalIgnoreCase))
                {
                    return "correlation_failed";
                }

                if (current.Message.Contains("state", StringComparison.OrdinalIgnoreCase)
                    && (current.Message.Contains("invalid", StringComparison.OrdinalIgnoreCase)
                        || current.Message.Contains("missing", StringComparison.OrdinalIgnoreCase)))
                {
                    return "invalid_state";
                }
            }

            return "unclassified_remote_failure";
        }
    }
}
