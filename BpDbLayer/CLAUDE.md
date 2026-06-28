# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build
```

There are no tests in this project. Migrations do **not** live here — they live in `DbMigration.MsSql` / `DbMigration.MySql`.

## Migrations

Migrations are managed by a custom external tool (`EfDbTool.exe`) located at `D:\Develop\CSharpProjects\EfDbTool`. Run the batch scripts in `EfUtils\`:

```
EfUtils\MigrationsDevelopLocal.bat   # generate a new migration (local dev DB)
EfUtils\UpdateDevelopLocal.bat        # apply pending migrations (local dev DB)
EfUtils\MigrationsDevelop.bat         # generate migration (shared dev DB)
EfUtils\UpdateDevelop.bat             # apply migrations (shared dev DB)
```

When adding a new entity, also register it in `BpDbContext` and create a corresponding `IEntityTypeConfiguration<T>` in `ModelConfigurations/`.

## Architecture

### Entity conventions

All models are `partial` classes in `Models/`. Two audit interfaces drive timestamp/author tracking:

- `ICreatedFields` — `CreatedDate`, `CreatedByUserId` (public interface)
- `IModifiedFields` — `ModifiedDate`, `ModifiedByUserId` (internal interface)

Not every entity implements both — check individual models. Data annotations (`[Key]`, `[Required]`, `[StringLength]`) are used for basic constraints; anything requiring relationships, unique indexes, or seed data belongs in a `ModelConfigurations/XxxConfiguration.cs` class implementing `IEntityTypeConfiguration<T>`. All configurations are auto-registered via `ApplyConfigurationsFromAssembly` in `BpDbContext.OnModelCreating`.

### Seeded data

`BpUserConfiguration` seeds an admin user (ID = `BpUserConfiguration.AdminUserId = 1`, username `admin`, password `admin` hashed with Argon2id). This constant is also used as the default `CreatedByUserId`/`ModifiedByUserId` in migration seed data for other entities.

### Password hashing

`Utils/PasswordHasher.cs` provides `HashPassword(string)` and `VerifyPassword(string, string)` using **Argon2id** (`Konscious.Security.Cryptography.Argon2`). Parameters: 16-byte salt, 32-byte hash, 4 iterations, 64 MB memory, parallelism 2. The stored format is `base64(salt + hash)`. Use `CryptographicOperations.FixedTimeEquals` for comparison (already done internally).

### `Secrets/` folder

The `.csproj` explicitly excludes `Secrets/**` from compilation. This folder is gitignored and intended for local secret overrides — do not add files there that should be committed.
