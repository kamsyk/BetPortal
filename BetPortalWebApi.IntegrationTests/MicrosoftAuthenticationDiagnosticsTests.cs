using BetPortalWebApi.Utils;

public sealed class MicrosoftAuthenticationDiagnosticsTests
{
    [Theory]
    [InlineData("AADSTS7000215: Invalid client secret provided. secret=synthetic-private-value", "AADSTS7000215")]
    [InlineData("invalid_client; client_secret=synthetic-private-value", "invalid_client")]
    [InlineData("invalid_client; AADSTS7000215: Invalid client secret; synthetic-private-value", "AADSTS7000215")]
    [InlineData("Correlation failed.", "correlation_failed")]
    [InlineData("The oauth state was missing or invalid.", "invalid_state")]
    [InlineData("access_denied; code=synthetic-private-value", "access_denied")]
    [InlineData("Unrecognized failure: synthetic-private-value", "unclassified_remote_failure")]
    public void Returns_only_safe_diagnostic_codes(string message, string expected)
    {
        var result = MicrosoftAuthenticationDiagnostics.GetFailureCode(new InvalidOperationException(message));
        Assert.Equal(expected, result);
        Assert.DoesNotContain("synthetic-private-value", result);
    }

    [Fact]
    public void Checks_inner_exceptions_without_returning_raw_messages()
    {
        var failure = new InvalidOperationException(
            "Remote failure",
            new InvalidOperationException("AADSTS7000222: expired secret; synthetic-private-value"));
        Assert.Equal("AADSTS7000222", MicrosoftAuthenticationDiagnostics.GetFailureCode(failure));
    }

    [Fact]
    public void Missing_failure_is_explicitly_unclassified()
    {
        Assert.Equal("unclassified_remote_failure", MicrosoftAuthenticationDiagnostics.GetFailureCode(null));
    }
}
