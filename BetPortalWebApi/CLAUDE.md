# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet restore
dotnet build
dotnet run          # http://localhost:5026 — Swagger UI at /swagger (development only)
```

There are no automated tests in this project yet.

## Configuration

`appsettings.Development.json` holds the local SQL Server connection string and all secrets. The project has `UserSecretsId` configured — use `dotnet user-secrets` for sensitive values rather than committing them to `appsettings.json`:

```bash
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."
```

Dev CORS allows origins: `http://localhost:4200` (bet-portal), `http://localhost:4210` (bet-portal-admin), `http://localhost:5026`.

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
- `SessionService` refreshes permissions from DB every 10 minutes of session lifetime — currently all users always receive `["AdminAccess"]` (hardcoded, marked TODO).
- `UserService` is registered as **singleton** because it is stateless. All other services are **scoped**.

### External dependency

`KsAuthentication` is referenced via a relative path (`..\..\KsAuthentication\`) and must exist on disk. It provides the `JwtUser` contract used by `IUserService`.

### StyleCop

For C# development and StyleCop warning fixes, use the [betportal-api-stylecop skill](../.claude/skills/betportal-api-stylecop/SKILL.md).

StyleCop.Analyzers is active. [stylecop.json](./stylecop.json) disables the listed documentation requirements, not every documentation diagnostic. Rules not configured there retain analyzer defaults, subject to severity overrides in [the repository .editorconfig](../.editorconfig). Key ordering rules enforced:
- `using` directives outside the namespace declaration.
- `System.*` usings before other usings.

With the current analyzer version, separate adjacent top-level statements in `Program.cs` with blank lines. Missing separators can produce a locationless `CSC : warning SA1516`.
