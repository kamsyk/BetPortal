using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;

internal static partial class ExternalAuthAssert
{
    /// <summary>Asserts a browser-friendly redirect to the portal login dialog instead of a JSON error.</summary>
    public static void LoginErrorRedirect(HttpResponseMessage response, string errorParameter, string expectedCode, string expectedReturnUrl)
    {
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost:4200/login", response.Headers.Location!.GetLeftPart(UriPartial.Path));
        var query = QueryHelpers.ParseQuery(response.Headers.Location.Query);
        Assert.Equal(expectedCode, query[errorParameter]);
        Assert.Equal(expectedReturnUrl, query["returnUrl"]);
        if (expectedCode is "account-required" or "account-exists")
        {
            Assert.False(query.ContainsKey("errorId"));
        }
        else
        {
            Assert.Matches(ErrorIdPattern(), query["errorId"].ToString());
        }
    }

    /// <summary>Asserts Problem Details carry only a support reference and no trace or exception details.</summary>
    public static async Task<JsonElement> SanitizedProblemAsync(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Equal((int)expectedStatus, body.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("title").GetString()));
        Assert.Matches(ErrorIdPattern(), body.GetProperty("errorId").GetString());
        Assert.False(body.TryGetProperty("traceId", out _));
        Assert.False(body.TryGetProperty("exception", out _));
        Assert.False(body.TryGetProperty("stackTrace", out _));
        return body;
    }

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex ErrorIdPattern();
}
