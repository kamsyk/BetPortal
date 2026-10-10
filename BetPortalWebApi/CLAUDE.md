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

Google sign-in (`GET /api/auth/google?returnUrl=/path`) is enabled only when `Authentication:Google:ClientId` and `Authentication:Google:ClientSecret` are set; otherwise it returns 503. `Authentication:Google:FrontendOrigin` (for example `http://localhost:4200`) is the only origin it redirects back to. In the Google Cloud OAuth client, register the redirect URI `http://localhost:5026/signin-google`. Existing active users are matched by verified email; new users are created without roles, so they do not get `AdminAccess`.

`/signin-google` is handled by the Google authentication middleware, which exchanges the authorization code and reads Google's v3 user-info response. Its `email_verified` field must be mapped to the verification claim. The middleware then redirects to `/api/auth/google/callback`, which validates the external ticket, sets the application cookies, and redirects to the portal's `returnUrl` (the home page `/` by default).

```bash
dotnet user-secrets set "Authentication:Google:ClientId" "<client-id>.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<client-secret>"
```

## Architecture

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
```

### Adding a new CRUD endpoint

All list endpoints follow the same pattern — see `SportController` as the reference implementation:

1. Define **static** `FilterHandlers` and `SortHandlers` dictionaries on the controller (keyed by query-param name / column name).
2. Call `ListQueryProcessor.Apply(query, request, HttpContext.Request.Query, FilterHandlers, SortHandlers, defaultSortKey)` to apply filters and sorting from query parameters.
3. `ListQueryRequest` (`[FromQuery]`) carries `SortBy` / `SortOrder`; individual filter values are read directly from the query string by `ListQueryProcessor`.
4. Define DTOs as nested classes inside the controller (not in `Contracts/`).

Write-endpoints (`POST add`, `PUT {id}`, `DELETE {id}`) require `[Authorize(Policy = "AdminAccess")]`. Read-endpoints use `[Authorize]` only.

### Authentication

Dual-scheme setup in `Program.cs` (see solution-level `CLAUDE.md` for the full picture):

- **Sessions are in-memory only** (`IMemoryCache`). Sessions are lost on process restart; the `AuthCookie` durable cookie will then re-authenticate via `CookieAuth`.
- `SessionService` derives permissions from the user's active `Admin` role (`BpUsers_BpRoles`): admins get `["AdminAccess"]`, everyone else gets none. `CookieAuth` re-checks the user and permissions on every request.
- `UserService` is registered as **singleton** because it is stateless. All other services are **scoped**.

### External dependency

`KsAuthentication` is referenced via a relative path (`..\..\KsAuthentication\`) and must exist on disk. It provides the `JwtUser` contract used by `IUserService`.

### StyleCop

For C# development and StyleCop warning fixes, use the [betportal-api-stylecop skill](../.claude/skills/betportal-api-stylecop/SKILL.md).

StyleCop.Analyzers is active. [stylecop.json](./stylecop.json) disables the listed documentation requirements, not every documentation diagnostic. Rules not configured there retain analyzer defaults, subject to severity overrides in [the repository .editorconfig](../.editorconfig). Key ordering rules enforced:
- `using` directives outside the namespace declaration.
- `System.*` usings before other usings.

With the current analyzer version, separate adjacent top-level statements in `Program.cs` with blank lines. Missing separators can produce a locationless `CSC : warning SA1516`.
