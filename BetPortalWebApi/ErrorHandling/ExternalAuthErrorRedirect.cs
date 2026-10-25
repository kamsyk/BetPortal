using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.WebUtilities;

namespace BetPortalWebApi.ErrorHandling
{
    /// <summary>Builds safe redirects from external sign-in failures back to the portal login dialog.</summary>
    public static class ExternalAuthErrorRedirect
    {
        public static readonly IReadOnlyList<string> Providers = ["Google", "Facebook", "Microsoft"];

        public static bool TryGetFrontendOrigin(IConfiguration configuration, string provider, [NotNullWhen(true)] out Uri? origin)
        {
            if (Uri.TryCreate(configuration[$"Authentication:{provider}:FrontendOrigin"], UriKind.Absolute, out origin)
                && (origin.Scheme == Uri.UriSchemeHttps || origin.Scheme == Uri.UriSchemeHttp)
                && string.IsNullOrEmpty(origin.UserInfo)
                && origin.AbsolutePath == "/"
                && string.IsNullOrEmpty(origin.Query)
                && string.IsNullOrEmpty(origin.Fragment))
            {
                return true;
            }

            origin = null;
            return false;
        }

        public static bool TryGetProvider(PathString path, [NotNullWhen(true)] out string? provider)
        {
            foreach (var candidate in Providers)
            {
                var name = candidate.ToLowerInvariant();
                if (path.StartsWithSegments($"/api/auth/{name}", StringComparison.OrdinalIgnoreCase)
                    || path.StartsWithSegments($"/signin-{name}", StringComparison.OrdinalIgnoreCase))
                {
                    provider = candidate;
                    return true;
                }
            }

            provider = null;
            return false;
        }

        /// <summary>
        /// Creates <c>{origin}/login?{provider}Error=code&amp;returnUrl=path[&amp;errorId=id]</c>. A destination outside the
        /// configured frontend origin is replaced by <c>/</c>.
        /// </summary>
        public static bool TryCreate(
            IConfiguration configuration,
            string provider,
            string errorCode,
            string? errorId,
            string? destination,
            [NotNullWhen(true)] out string? url)
        {
            url = null;
            if (!TryGetFrontendOrigin(configuration, provider, out var origin))
            {
                return false;
            }

            var returnUrl = "/";
            if (Uri.TryCreate(destination, UriKind.Absolute, out var target)
                && target.GetLeftPart(UriPartial.Authority) == origin.GetLeftPart(UriPartial.Authority)
                && string.IsNullOrEmpty(target.UserInfo))
            {
                returnUrl = target.PathAndQuery + target.Fragment;
            }

            var query = new Dictionary<string, string?>
            {
                [$"{provider.ToLowerInvariant()}Error"] = errorCode,
                ["returnUrl"] = returnUrl,
            };
            if (!string.IsNullOrEmpty(errorId))
            {
                query["errorId"] = errorId;
            }

            url = QueryHelpers.AddQueryString(new Uri(origin, "/login").AbsoluteUri, query);
            return true;
        }
    }
}
