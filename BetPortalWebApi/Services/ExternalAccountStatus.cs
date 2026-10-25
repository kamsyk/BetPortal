namespace BetPortalWebApi.Services
{
    public enum ExternalAccountStatus
    {
        /// <summary>An active account is linked to the provider email.</summary>
        Found,

        /// <summary>No local account or provider link uses the email.</summary>
        NotFound,

        /// <summary>A local account uses the email, but it is not linked to this provider.</summary>
        NotLinked,

        /// <summary>The provider link belongs to an account that has not been activated.</summary>
        Inactive,

        /// <summary>The provider email is linked to more than one account.</summary>
        Ambiguous,
    }
}
