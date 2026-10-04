# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Solution Layout

| Project | Tech | Role |
|---------|------|------|
| `BetPortalWebApi` | ASP.NET Core 9, EF Core | REST API |
| `BpDbLayer` | EF Core, SQL Server | DB context, models, migrations |
| `DbMigration.MsSql` / `DbMigration.MySql` | EF Core | Migration assemblies |
| `bet-portal` | Angular 20, SSR | Public-facing portal (port 4200) |
| `bet-portal-admin` | Angular 20, SSR | Admin portal (port 4210) |

The API runs on `http://localhost:5026`. Both Angular apps call it via `API_BASE_URL` injection token (set per environment in `src/environments/`).

## Commands

### Backend
```bash
cd BetPortalWebApi
dotnet restore
dotnet build
dotnet run           # http://localhost:5026, Swagger at /swagger
```

### Angular frontends
```bash
cd bet-portal          # or bet-portal-admin
npm install
npm start              # dev server
npm run build          # SSR build → dist/
npm test               # Karma/Jasmine tests
ng test --include='**/foo.component.spec.ts'   # run single spec
npm run lint           # ESLint
node dist/bet-portal/server/server.mjs   # serve SSR build
```

### Database migrations (uses custom EfDbTool at `D:\Develop\CSharpProjects\EfDbTool`)
```bash
# From BpDbLayer\EfUtils\:
MigrationsDevelopLocal.bat   # create migration
UpdateDevelopLocal.bat        # apply migration to local dev DB

# Or directly via dotnet ef (MsSql):
cd DbMigration.MsSql
dotnet ef migrations add <Name> --context BpDbContext
dotnet ef database update --context BpDbContext
```

## Architecture

### Authentication
Dual-scheme auth in `Program.cs`:
- **`CookieAuth`** — durable `AuthCookie` cookie, 7-day sliding expiration. Primary scheme for persistent sessions.
- **`SessionAuth`** — legacy `SessionId` cookie validated by `SessionAuthenticationHandler` → `ISessionService`.
- **`DynamicAuth`** policy scheme (default) — selects `CookieAuth` when `AuthCookie` cookie is present, otherwise falls back to `SessionAuth`.

Login writes both cookies (`SessionId` + `AuthCookie`). The `GET /api/auth/check` endpoint validates whichever session exists and re-issues `CookieAuth` if session is valid.

`AdminAccess` policy requires claim `Permission = "AdminAccess"` (set at login for all users currently).

### Angular auth flow
`authGuard` calls `AuthService.checkAuthStatus()` (from `ks-ag-controls`) → `GET /api/auth/check`. `authInterceptor` adds `withCredentials: true` to every request; on SSR it forwards the raw `Cookie` header from the incoming request via `global.__request__`.

### `ks-ag-controls` local library
Both Angular apps depend on `ks-ag-controls` installed from a local `.tgz`:
```
"ks-ag-controls": "file:../../../Angular/ks-ag-controls/ks-ag-controls-workspace/dist/ks-ag-controls/ks-ag-controls-0.0.6.tgz"
```
Scripts `install-ks-controls-dev.bat` / `install-ks-controls-prod.bat` in each Angular project update this package. `AuthService` (used by auth guard) lives in this library.

### External project dependency
`BetPortalWebApi` references `KsAuthentication` via a relative path outside this repo:
```
..\..\KsAuthentication\KsAuthentication\KsAuthentication.csproj
```
This must be cloned/built alongside `BetPortal` or the API won't compile.

### Claude AI chatbot (`/advisor` in bet-portal)
`ChatService` calls the Anthropic API directly via `HttpClient` (not the SDK). `DbQueryService` fetches live DB context passed as a system-prompt prefix. Config keys: `Claude:ApiKey`, `Claude:Model`, `Claude:MaxTokens`.

**The `Claude:ApiKey` must not be committed in `appsettings.json`. Use `dotnet user-secrets`:**
```bash
cd BetPortalWebApi
dotnet user-secrets set "Claude:ApiKey" "sk-ant-..."
```

### ag-Grid settings persistence (`bet-portal-admin`)
`GridSettingsService` / `GET|POST|DELETE /api/gridsettings/{pageIdentifier}` persists column state, filters, and page size per user per page in `UserGridSettings` table. Components debounce saves by 1 s and apply saved settings in `onGridReady`. See `GRID_SETTINGS_README.md` for integration pattern.

### DB model conventions
All entities implement `ICreatedFields` / `IModifiedFields` (CreatedDate, CreatedByUserId, ModifiedDate, ModifiedByUserId). EF config lives in `ModelConfigurations/` using `IEntityTypeConfiguration<T>`.

## API C# Coding Rules

When creating, editing, refactoring, or reviewing `BetPortalWebApi` C# code, use the [betportal-api-stylecop skill](.claude/skills/betportal-api-stylecop/SKILL.md). It reflects the API's [stylecop.json](BetPortalWebApi/stylecop.json), the installed analyzer, and repository diagnostic overrides, including the blank-line requirements for top-level statements.

## Angular Coding Rules

Full rules are in `bet-portal/.claude/CLAUDE.md` and `bet-portal-admin/.claude/CLAUDE.md`. Key non-obvious points:

- Do **not** set `standalone: true` — default in Angular v20+.
- `ChangeDetectionStrategy.OnPush` on every component.
- `input()` / `output()` functions instead of `@Input()` / `@Output()` decorators.
- Host bindings in the `host` object of `@Component`, not `@HostBinding`/`@HostListener`.
- `class` / `style` bindings instead of `ngClass` / `ngStyle`.
- Native control flow (`@if`, `@for`, `@switch`) — not structural directives.
- Signals for state; `computed()` for derived values; never `mutate()`.
- `inject()` instead of constructor injection.
- All feature routes lazy-loaded via `loadComponent()`.
- Must pass AXE checks and WCAG AA.
