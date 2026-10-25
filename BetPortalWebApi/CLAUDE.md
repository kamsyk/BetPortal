# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet restore
dotnet build
dotnet run          # http://localhost:5026 — Swagger UI at /swagger (development only)
```

The API integration tests live in [BetPortalWebApi.IntegrationTests](../BetPortalWebApi.IntegrationTests/). They use xUnit v3, `WebApplicationFactory<Program>`, and real SQL Server migrations against a unique SQL Server LocalDB database per fixture. LocalDB must be installed and the `MSSQLLocalDB` instance available. The fixture creates and drops only its `BetPortal_IntegrationTests_<guid>` database; tests override the application connection string and never use the development database. Run from the repository root: `dotnet test BetPortalWebApi.IntegrationTests\BetPortalWebApi.IntegrationTests.csproj`.

## Configuration

`appsettings.Development.json` holds the local SQL Server connection string and all secrets. The project has `UserSecretsId` configured — use `dotnet user-secrets` for sensitive values rather than committing them to `appsettings.json`:

```bash
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."
```

Dev CORS allows origins: `http://localhost:4200` (bet-portal), `http://localhost:4210` (bet-portal-admin), `http://localhost:5026`.

`IEmailService` is backed by the singleton `AzureCommunicationEmailService`. Configure `AzureCommunication:ConnectionString` and `AzureCommunication:SenderAddress` (an Azure-verified sender) using user secrets or environment variables. Missing/placeholder/invalid configuration disables delivery with a logged error or warning; sends return `false`. The service supports HTML, optional plain text and BCC, and cancellation. It returns `true` only when Azure's send operation succeeds (not proof of inbox delivery). Azure request failures are logged and return `false`; cancellation and unexpected failures propagate. Google registration uses this service to send its welcome/activation email.

```bash
dotnet user-secrets set "AzureCommunication:ConnectionString" "<azure-communication-connection-string>"
dotnet user-secrets set "AzureCommunication:SenderAddress" "<verified-sender-address>"
```

Google sign-in (`GET /api/auth/google?returnUrl=/path`) is enabled only when `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` are set; otherwise it redirects with `googleError=not-configured` (503 Problem Details only if no frontend origin is configured). `Authentication:Google:FrontendOrigin` (for example `http://localhost:4200`) is the only origin it redirects back to. In the Google Cloud OAuth client, register the redirect URI `http://localhost:5026/signin-google`. Existing active users are matched by verified email (case-insensitively) against a `BpUserLoginProvider` whose `LoginProvider.ProviderName` is `Google`. The provider-link email may differ from the local user's email. Matching only a local user email does not authorize Google login or automatically link that account. Unknown emails redirect to `/login?googleError=account-required&returnUrl=...` without creating users or application sessions. Inactive, unlinked, or ambiguous accounts are rejected with `account-inactive`, `account-not-linked`, or `account-unavailable` (see Error handling). The callback accepts emails up to 150 characters, matching the provider-link column.

`/signin-google` is handled by the Google authentication middleware, which exchanges the authorization code and reads Google's v3 user-info response. Its `email_verified` field must be mapped to the verification claim. The middleware then redirects to `/api/auth/google/callback`, which validates the external ticket, sets the application cookies, and redirects to the portal's `returnUrl` (the home page `/` by default).

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
```

## Architecture

### Microsoft authentication

For remote OAuth failures, the `MicrosoftAuthentication` logger records a sanitized `DiagnosticCode` (an AADSTS code, a known OAuth error, `correlation_failed`, `invalid_state`, or `unclassified_remote_failure`). The same sanitized classifier is used for Google and Facebook; the user is redirected with `{provider}Error=sign-in-failed`. Raw exception messages, credentials, tokens, and callback query values are not logged by this diagnostic handler. Restart the API and begin a fresh portal login attempt after configuration changes; do not reload an already-consumed callback URL.

Microsoft login starts at `GET /api/auth/microsoft?returnUrl=/path`; sign-up starts at `GET /api/auth/microsoft/register`. Register an application in Microsoft Entra with supported account types **Accounts in any organizational directory and personal Microsoft accounts**, add the **Web** redirect URI `http://localhost:5026/signin-microsoft` (use the exact HTTPS backend URI in production), and enable delegated Microsoft Graph `User.Read`. Use the client secret **value**, not its ID:

```bash
dotnet user-secrets set "Authentication:Microsoft:ClientId" "<application-client-id>"
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "<client-secret-value>"
dotnet user-secrets set "Authentication:Microsoft:FrontendOrigin" "http://localhost:4200"
```

The MicrosoftAccount handler uses Microsoft's `common` v2 endpoint for personal and work/school accounts, PKCE, OAuth correlation protection, and Graph `/v1.0/me`. The protected five-minute `MicrosoftExternal` cookie is separate from application and other provider cookies. `/api/auth/microsoft/callback` requires a provider ID and Graph `mail` of at most 150 characters. It deliberately does not fall back to `userPrincipalName`: a work/school username need not be a deliverable mailbox. Accounts without `mail` redirect with `email-unavailable`; invalid tickets, cancellation, or invalid state redirect with `sign-in-failed`. No application session is created.

Apply SQL Server migration `20261024210000_AddMicrosoftLoginProvider` before registration. It inserts the `Microsoft` provider without changing existing Google/Facebook records; no schema snapshot change is needed. This repository has no MySQL migration baseline; a MySQL deployment must provision the equivalent provider row using its deployment migration process.

`MicrosoftAccountService` reuses `ExternalAccountService`: login requires an active, unambiguous Microsoft provider link matched by email, case-insensitively. Local email matches or other providers never automatically authorize or link Microsoft login. Unknown accounts redirect to `/login?microsoftError=account-required&returnUrl=...`; existing unlinked/inactive/ambiguous accounts redirect with `account-not-linked`/`account-inactive`/`account-unavailable`. Explicit sign-up atomically saves an inactive user, activation token, and Microsoft link, sends an activation email, and redirects to `/sign-up-confirmation` without signing in. Duplicate accounts redirect with `microsoftError=account-exists`; email delivery failure preserves the inactive account and redirects with `activation-email-failed`. The existing Azure email configuration is required. Return URLs are restricted to `Authentication:Microsoft:FrontendOrigin`. The public portal supports Microsoft on both dialogs, including localized errors in English, Czech, and German.

### Facebook authentication

Facebook login starts at `GET /api/auth/facebook?returnUrl=/path`; sign-up starts at `GET /api/auth/facebook/register`. Set `Authentication:Facebook:AppId`, `Authentication:Facebook:AppSecret`, and `Authentication:Facebook:FrontendOrigin`. Development uses `http://localhost:4200` as the frontend origin. Keep secrets in user secrets or environment variables, never in committed configuration. In Meta's Facebook Login settings, allow the exact backend callback URL (`http://localhost:5026/signin-facebook` locally, or the HTTPS backend URL used by your app); use HTTPS where required by Meta. Development-mode apps only allow app administrators/developers/testers; production access and the email permission may require Meta review.

The ASP.NET Facebook handler requests the `email` permission, exchanges the code server-side, and fetches `id`, `email`, `first_name`, and `last_name` from Graph API with an app-secret proof. Its protected, five-minute `FacebookExternal` cookie is separate from Google and application cookies. `/api/auth/facebook/callback` requires an authenticated Facebook ticket with an ID and email of at most 150 characters. Missing email (including declined email permission) redirects with `email-unavailable`; cancellation or invalid OAuth state redirects with `sign-in-failed`. No application session is created. Facebook does not supply Google's `email_verified` claim; registration's activation email verifies mailbox access.

`FacebookAccountService` and `GoogleAccountService` share `ExternalAccountService`. Facebook registration creates an inactive user, activation token, and `BpUserLoginProvider` for the existing `Facebook` provider in one transaction, then sends the activation email. Registration never logs the user in; delivery failure preserves the saved records and redirects with `activation-email-failed`. Apply the existing login-provider migration, which inserts Google and Facebook, before use. Existing local or same-provider emails are rejected without linking or modification and redirect to `/login?facebookError=account-exists`.

Facebook login requires an active, unambiguous Facebook provider link matched by email, case-insensitively. A local email or Google link alone does not grant Facebook access. Unknown accounts redirect to `/login?facebookError=account-required`, where the public portal opens Sign up. Inactive or unlinked accounts redirect with `account-inactive` or `account-not-linked`. No automatic cross-provider linking is performed. All return URLs stay on the provider's configured frontend origin. See `ExternalAuth.http` for request examples.

Account activation uses anonymous `POST /api/user-token` with JSON `{ "token": "..." }`. A case-sensitive, unambiguous token match activates its user, updates audit fields, and deletes the token in one transaction. Tokens are single-use with no expiry. Missing/malformed tokens return 400; unknown, ambiguous, or consumed tokens return 404. Activation does not issue authentication cookies or assign roles.

Google registration starts at `/api/auth/google/register` and stores registration intent in the protected OAuth state. After Google verifies the email, it creates an inactive user, a `BpUserToken` with a cryptographically random 64-character token, and a `BpUserLoginProvider` containing the verified email and the existing `Google` provider in one transaction. After committing, it sends a "Welcome to Bet Portal" email (HTML and plain text) containing `${Authentication:Google:FrontendOrigin}/user-token?token=<token>`, then redirects to `/sign-up-confirmation` without issuing application auth cookies. Names and URLs in HTML are encoded. Existing emails are rejected without changes or another email and return to Login with `googleError=account-exists`. If email delivery returns failure, the inactive account/token remain saved and the callback redirects with `googleError=activation-email-failed` and an error reference so the user can contact support. No automatic retry/resend is implemented. Registration itself does not activate the account.

### Error handling and logging

Users never see exception messages, stack traces, or raw trace data.

- **Unhandled exceptions**: `ErrorHandling/GlobalExceptionHandler` (registered with `AddExceptionHandler` + `UseExceptionHandler`) logs the exception and returns a 500 Problem Details with a generic title/detail and an `errorId`. GET requests to external sign-in paths (`/api/auth/{google|facebook|microsoft}*`, `/signin-*`) redirect to the portal with `sign-in-failed` instead.
- **Problem Details**: `AddProblemDetails` replaces the default `traceId` extension with `errorId` on every Problem Details response. `errorId` is the 32-character W3C trace id and equals the `TraceId` column of the log table, so support can look up the full entry.
- **External sign-in**: every failure is a redirect to `{FrontendOrigin}/login?{provider}Error=<code>&returnUrl=<path>&errorId=<id>` (no `errorId` for `account-required`/`account-exists`). Codes are defined in `ErrorHandling/ExternalAuthErrorCodes`: `account-required`, `account-exists`, `account-not-linked` (email belongs to an account that uses another sign-in method), `account-inactive`, `account-unavailable` (ambiguous provider link), `email-unavailable`, `sign-in-failed`, `activation-email-failed`, `not-configured`. The public portal localizes these messages and shows the reference. For `account-not-linked`, the redirect also carries `errorDetails`: an opaque token encrypted with ASP.NET Data Protection (`ExternalAuthErrorDetailsService`, valid for 10 minutes). The login page exchanges it at anonymous `GET /api/auth/external-error-details?token=...` for `{ email, signInMethods }` (`no-store`; invalid or expired tokens return 404 Problem Details). The email is masked on the server (`k***@gmail.com`), and `signInMethods` lists `Password` (when the account has a password) plus the providers linked to the account, so the full email never appears in the URL and the guidance cannot be forged.
- **Logging**: Serilog (`Serilog.AspNetCore` 9.x) replaces the default logger providers; levels are configured in the `Serilog` section. Console/Debug sinks receive all enabled events. The MSSqlServer sink (`Serilog.Sinks.MSSqlServer` 8.x, pinned to stay on Microsoft.Extensions 9 / SqlClient 5) writes events at or above `DatabaseLogging:MinimumLevel` (default `Warning`) to `dbo.BpAppLogs` in batches. Columns: `TimeStamp` (UTC), `Level`, `Message`, `MessageTemplate`, `Exception`, `LogEvent` (JSON properties), `TraceId`, `SpanId`, `SourceContext`, `RequestPath`, `UserId` (authenticated user). Set `DatabaseLogging:Enabled` to `false` to disable the sink.
- Apply migration `20261025090000_AddAppLogTable` before running the API; the sink does not auto-create the table. The table is not part of the EF model, so the model snapshot is unchanged.

Find an entry for a user-reported reference:

```sql
SELECT * FROM dbo.BpAppLogs WHERE TraceId = '<errorId>' ORDER BY TimeStamp;
```

### Project structure

```
Controllers/        # API controllers + inline request/DTO classes
Contracts/          # Shared request/response types used across controllers
  Chat/
  GridSettings/
  List/             # ListQueryRequest (SortBy, SortOrder)
  User/
Services/           # Service implementations
  Interfaces/       # IXxx interfaces
Middleware/         # SessionAuthenticationHandler, UserSession
Utils/              # ListQueryProcessor
ErrorHandling/      # Global exception handler, Problem Details, external sign-in error redirects
Logging/            # Serilog configuration and user log context
```

### Adding a new CRUD endpoint

All list endpoints follow the same pattern — see `SportController` as the reference implementation:

1. Define **static** `FilterHandlers` and `SortHandlers` dictionaries on the controller (keyed by query-param name / column name).
2. Call `ListQueryProcessor.Apply(query, request, HttpContext.Request.Query, FilterHandlers, SortHandlers, defaultSortKey)` to apply filters and sorting from query parameters.
3. `ListQueryRequest` (`[FromQuery]`) carries `SortBy` / `SortOrder`; individual filter values are read directly from the query string by `ListQueryProcessor`.
4. Define DTOs as nested classes inside the controller (not in `Contracts/`).

Write-endpoints (`POST add`, `PUT {id}`, `DELETE {id}`) require `[Authorize(Policy = "AdminAccess")]`. Read-endpoints use `[Authorize]` only.

### Authentication

Authentication setup in `Program.cs` (see solution-level `CLAUDE.md` for the full picture):

- Sessions are persisted in `BpUserSessions`; only a SHA-256 hash of the 32-byte random `SessionId` token is stored. The cookie is HttpOnly, SameSite=Lax, Secure on HTTPS, scoped to `/`, and expires after 24 hours.
- `SessionAuthenticationHandler` checks the session expiration and current user active status on every request. `SessionService` derives current permissions from the active `Admin` role (`BpUsers_BpRoles`): admins get `["AdminAccess"]`, everyone else gets none.
- Logout revokes the database session. Expired rows are removed hourly by `SessionCleanupBackgroundService`. `AuthCookie` is no longer an authentication scheme; requests carrying a legacy `AuthCookie` have it cleared.
- Apply SQL Server migration `20261025112822_AddPersistentSessions` before deploying this version. The API currently uses SQL Server; the MySQL migration project has no migration baseline and must provision the equivalent `BpUserSessions` table through its deployment process before being used with this model.
- `UserService` is registered as **singleton** because it is stateless. All other services are **scoped**.

### External dependency

`KsAuthentication` is referenced via a relative path (`..\..\KsAuthentication\`) and must exist on disk. It provides the `JwtUser` contract used by `IUserService`.

### StyleCop

For C# development and StyleCop warning fixes, use the [betportal-api-stylecop skill](../.claude/skills/betportal-api-stylecop/SKILL.md).

StyleCop.Analyzers is active. [stylecop.json](./stylecop.json) disables the listed documentation requirements, not every documentation diagnostic. Rules not configured there retain analyzer defaults, subject to severity overrides in [the repository .editorconfig](../.editorconfig). Key ordering rules enforced:
- `using` directives outside the namespace declaration.
- `System.*` usings before other usings.

With the current analyzer version, separate adjacent top-level statements in `Program.cs` with blank lines. Missing separators can produce a locationless `CSC : warning SA1516`.
