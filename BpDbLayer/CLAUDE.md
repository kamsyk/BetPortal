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

`Competitors` is registered in the runtime context and both migration contexts. They share `CompetitorConfiguration`: a required, non-unique name (100 characters), a required website (2,048 characters), and required creator/modifier foreign keys to `BpUser` with restricted deletes. Generate and review a provider-specific migration before using the table; registering the entity does not update an existing database.

## Architecture

### User favorites

`BpUserFavorites` stores one favorite per row with a generated `Id`, required owner and audit fields, and exactly one non-null target (`CountryId`, `SportId`, `CompetitionId`, or `CompetitorId`). A database check constraint enforces the single target; four unique owner/target indexes prevent duplicates. SQL Server generates filtered indexes excluding null targets; MySQL unique indexes permit nulls. All seven relationships use restricted deletes. Delete favorites explicitly before deleting a referenced user or target. The runtime and both migration contexts use the shared configuration and expose the referenced entities. MySQL must support enforced check constraints (8.0.16+). Generate and review migrations before using these tables.

### Entity conventions

All models are `partial` classes in `Models/`. Two audit interfaces drive timestamp/author tracking:

- `ICreatedFields` — `CreatedDate`, `CreatedByUserId` (public interface)
- `IModifiedFields` — `ModifiedDate`, `ModifiedByUserId` (internal interface)

Not every entity implements both — check individual models. Data annotations (`[Key]`, `[Required]`, `[StringLength]`) are used for basic constraints; anything requiring relationships, unique indexes, or seed data belongs in a `ModelConfigurations/XxxConfiguration.cs` class implementing `IEntityTypeConfiguration<T>`. All configurations are auto-registered via `ApplyConfigurationsFromAssembly` in `BpDbContext.OnModelCreating`.

### Seeded data

`BpUserConfiguration` seeds an admin user (ID = `BpUserConfiguration.AdminUserId = 1`, username `admin`, password `admin` hashed with Argon2id). This constant is also used as the default `CreatedByUserId`/`ModifiedByUserId` in migration seed data for other entities.

`BpRoleConfiguration` seeds `Admin` (ID 1), `BasicUser` (ID 2), and `SubscribedUserL1` (ID 3). The latter two are inserted by SQL Server migration `20261010163051_BpMigration_20261010183037`; creating the roles does not assign them to users or grant new permissions.

### Password hashing

`Utils/PasswordHasher.cs` provides `HashPassword(string)` and `VerifyPassword(string, string)` using **Argon2id** (`Konscious.Security.Cryptography.Argon2`). Parameters: 16-byte salt, 32-byte hash, 4 iterations, 64 MB memory, parallelism 2. The stored format is `base64(salt + hash)`. Use `CryptographicOperations.FixedTimeEquals` for comparison (already done internally).

### `Secrets/` folder

The `.csproj` explicitly excludes `Secrets/**` from compilation. This folder is gitignored and intended for local secret overrides — do not add files there that should be committed.
