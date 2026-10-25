namespace BetPortalWebApi.ErrorHandling
{
    /// <summary>Error codes sent to the portal in the <c>{provider}Error</c> query parameter after external sign-in.</summary>
    public static class ExternalAuthErrorCodes
    {
        public const string AccountRequired = "account-required";

        public const string AccountExists = "account-exists";

        public const string AccountNotLinked = "account-not-linked";

        public const string AccountInactive = "account-inactive";

        public const string AccountUnavailable = "account-unavailable";

        public const string EmailUnavailable = "email-unavailable";

        public const string SignInFailed = "sign-in-failed";

        public const string ActivationEmailFailed = "activation-email-failed";

        public const string NotConfigured = "not-configured";
    }
}
