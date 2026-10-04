---
name: betportal-api-stylecop
description: Guides C# development in BetPortalWebApi using its StyleCop configuration. Use when creating, editing, refactoring, or reviewing API C# code, or fixing StyleCop build warnings, including locationless CSC SA1516 warnings.
---

# BetPortal API StyleCop

Apply this skill to C# changes in `BetPortalWebApi`. Do not apply the API's settings to other projects without checking their configuration.

## Read the effective configuration

Before editing, read these files relative to the repository root:

- [BetPortalWebApi/stylecop.json](../../../BetPortalWebApi/stylecop.json): explicit StyleCop settings.
- [BetPortalWebApi/BetPortalWebApi.csproj](../../../BetPortalWebApi/BetPortalWebApi.csproj): analyzer version and `AdditionalFiles` registration.
- [.editorconfig](../../../.editorconfig): formatting preferences and diagnostic severity overrides.
- [BetPortalWebApi/CLAUDE.md](../../../BetPortalWebApi/CLAUDE.md): project-specific development conventions.

Treat these files as the source of truth and reread them when they change. Settings not specified in `stylecop.json` retain the installed analyzer's defaults; the file is not a list of every enabled rule.

## Explicit StyleCop settings

The current `orderingRules` require:

- `systemUsingDirectivesFirst: true`: put `System` and `System.*` imports before non-System imports; sort imports within each group.
- `usingDirectivesPlacement: "outsideNamespace"`: place imports before the namespace declaration.

The current `documentationRules` set all of the following to `false`:

- `documentInterfaces`
- `documentExposedElements`
- `documentInternalElements`
- `documentPrivateElements`
- `documentPrivateFields`
- `xmlHeader`

Do not add XML documentation or file headers solely to satisfy these disabled requirements. Preserve useful existing documentation and document non-obvious behavior when needed. These options do **not** disable every documentation diagnostic or the other StyleCop rule categories.

The repository's `.editorconfig` currently sets `dotnet_sort_system_directives_first = false`, which conflicts with StyleCop's explicit System-first setting. For API code, keep imports System-first to satisfy StyleCop; do not run an import organizer that reverses them. Do not change repository-wide settings as part of an unrelated API task.

## Avoid recurring warnings

These practices follow the installed analyzer and existing API conventions, rather than additional settings in `stylecop.json`:

- Keep each top-level type in its own matching file (`SA1402`, `SA1649`). Preserve the API convention of nested controller DTOs.
- Use blank lines between declarations and members where required (`SA1516`).
- With the current `StyleCop.Analyzers` version `1.1.118`, separate adjacent top-level statements in `Program.cs` with blank lines, including service registrations, constants, and middleware calls. Missing separators can produce a single `CSC : warning SA1516` with no file or line location.
- Put explanatory loop comments on a separate line before the loop, not between the loop declaration and its opening brace (`SA1108`). Keep the required blank line before a standalone comment (`SA1515`).
- Follow `.editorconfig` for indentation and line endings. Avoid whole-file formatting or changes to generated files.
- Do not silence warnings with `NoWarn`, pragmas, severity changes, or an analyzer upgrade instead of fixing the underlying code.

## Verify C# changes

1. Inspect the diff to confirm only intended formatting or behavior changes.
2. From the repository root, build the API:

   ```powershell
   dotnet build BetPortalWebApi\BetPortalWebApi.csproj --no-restore
   ```

   Restore only when dependency changes or missing assets require it. Use `-t:Rebuild` when an incremental build might skip analyzer execution.

3. Fix any new warnings and errors. Run relevant existing tests when behavior changes.
4. If the output is locked by a running API, do not stop it without permission. Build with an isolated `OutputPath` in a temporary directory outside the project, then remove only that task's temporary output.
5. For a locationless `SA1516`, inspect spacing between top-level statements in `Program.cs` first. `dotnet format analyzers` may report no actionable diagnostics even when the build reports this warning; a successful format check is not a substitute for rebuilding.
6. Report the actual warning/error totals. Do not claim a warning-free build unless verified.

Documentation-only changes do not require a build.
