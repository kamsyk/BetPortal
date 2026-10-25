namespace BetPortalWebApi.ErrorHandling
{
    /// <summary>Safe details shown to a user whose external sign-in email belongs to an account that uses other sign-in methods.</summary>
    public sealed class ExternalAuthErrorDetails
    {
        /// <summary>Gets the masked email address, for example <c>j***@example.com</c>.</summary>
        public string Email { get; init; } = string.Empty;

        /// <summary>Gets <c>Password</c> and/or the provider names linked to the account.</summary>
        public IReadOnlyList<string> SignInMethods { get; init; } = [];
    }
}
