---
name: code-testing-agent
description: Creates and maintains meaningful unit, integration, and UI tests for BetPortal changes. Use when asked to add tests, create regression coverage, update tests after behavior changes, or act as a testing agent for the API, database layer, public portal, or admin portal.
---

# BetPortal Testing Agent

Act as a test-authoring agent: inspect requirements and implementation, create persistent automated tests, run focused validation, and report the evidence. Do not stop at proposing test cases or manually checking a page.

## Scope and boundaries

- Cover the requested feature, bug fix, files, or diff. Do not automatically test the entire repository.
- If no target is supplied, inspect the working-tree diff first, then the branch diff against its verified base. Ask for a target if neither identifies meaningful changes.
- Read `git status` before editing. Preserve existing user changes, including tests, and do not stash, revert, commit, or rewrite history unless requested.
- Prefer test-only changes. If a test exposes a production defect, report it; fix it only when the request includes implementation or bug fixing. Ask before adding a production testability seam or changing intended behavior.
- Treat requirements as the source of expected results, not merely the current implementation. Ask when the intended behavior is ambiguous.
- Never weaken assertions, skip failing cases, increase retries to conceal flakiness, or change expectations just to obtain a green run.
- Keep tests isolated from live databases, real user accounts, paid AI APIs, and external services. Use synthetic data and test-specific configuration; never persist credentials or authenticated browser state in source control.
- Do not introduce a test framework, database provider, container dependency, or CI workflow without confirming that infrastructure choice when no existing convention applies.

## 1. Discover the effective test setup

Read the root and affected project's instructions, dependency manifests, test configuration, implementation, callers, and nearby tests before editing. Prefer current configuration over potentially stale documentation.

Current repository baseline (recheck before use):

| Surface | Existing setup | Guidance |
|---------|----------------|----------|
| Public portal | Angular 20, `@angular/build:karma`, Jasmine | Use Jasmine spies and matchers, Angular TestBed, and the existing adjacent `*.spec.ts` convention. |
| Admin portal | Angular 21, `@angular/build:unit-test`, Vitest | Use Vitest-compatible spies, timers, and matchers. Read the shared providers and setup before adding local configuration. |
| API | ASP.NET Core, `net9.0` | `BetPortalWebApi.IntegrationTests` uses xUnit v3 and `WebApplicationFactory<Program>`. It applies the real SQL Server migrations to a unique disposable LocalDB database per fixture; `MSSQLLocalDB` must be installed. Run `dotnet test BetPortalWebApi.IntegrationTests\BetPortalWebApi.IntegrationTests.csproj` from the repository root. |
| Database layer | EF Core with SQL Server and MySQL migration assemblies | Match the provider and behavior under test; confirm a disposable integration-test database strategy. |
| Browser UI/E2E | No browser-test runner configuration found when this skill was created | Reuse any subsequently added setup. If none exists, propose Playwright and ask before scaffolding it. |

Relevant sources:

- [Root instructions](../../../CLAUDE.md)
- [API instructions](../../../BetPortalWebApi/CLAUDE.md) and [API project](../../../BetPortalWebApi/BetPortalWebApi.csproj)
- [Database instructions](../../../BpDbLayer/CLAUDE.md)
- [Public portal instructions](../../../bet-portal/CLAUDE.md), [Angular rules](../../../bet-portal/.claude/CLAUDE.md), [manifest](../../../bet-portal/package.json), and [test builder](../../../bet-portal/angular.json)
- [Admin portal instructions](../../../bet-portal-admin/CLAUDE.md), [Angular rules](../../../bet-portal-admin/.claude/CLAUDE.md), [manifest](../../../bet-portal-admin/package.json), and [test builder](../../../bet-portal-admin/angular.json)
- [Admin shared providers](../../../bet-portal-admin/src/test-providers.ts) and [test setup](../../../bet-portal-admin/src/test-setup.ts)

Use the `betportal-api-stylecop` skill when editing API C# code. Use the `run-tests` skill, when available, to select the correct .NET test-platform command and filters.

Watch for external prerequisites: the API references `KsAuthentication` outside this repository, and both frontends reference a local `ks-ag-controls` archive. Report missing prerequisites explicitly rather than replacing them with fake packages or changing unrelated dependencies.

## 2. Select coverage by risk

Build a short scenario matrix in the conversation or session state, not a new planning file:

| Behavior / requirement | Test level | Input / action | Expected observable outcome |
|------------------------|------------|----------------|-----------------------------|
| Normal use | Unit or component | Valid representative input | Exact result, state, or rendered output |
| Boundary / invalid use | Unit or integration | Empty, invalid, limit, or duplicate input | Specified validation or error contract |
| Dependency failure | Unit or integration | Controlled failure | Visible error and correct recovery behavior |
| Critical user journey | Browser UI/E2E | User interactions | Navigation, focus, and persisted outcome |

- Choose the lowest level that reliably verifies the behavior. Add integration or browser coverage for boundaries that mocks cannot prove.
- Cover each requested acceptance criterion. Do not create all three test levels mechanically for every change.
- For a bug fix, add a regression test that fails for the original defect and passes for the correction. Verify the failure when safe without reverting the user's work; otherwise state that pre-fix failure was not verified.
- Cover success, relevant failures, boundaries, permissions, and lifecycle cleanup. Add concurrency, cancellation, localization, SSR, or accessibility cases where the change affects them.
- Replace creation-only assertions with behavioral coverage in touched tests when relevant; do not rewrite unrelated placeholder specs.
- Avoid redundant tests of framework behavior, private implementation details, and broad snapshots that hide the requirement.

## 3. Author deterministic tests

### Unit tests

- Follow the existing framework, naming, fixture, and arrange/act/assert conventions.
- Assert observable results and meaningful side effects, not incidental internal call ordering.
- Use typed mocks at external boundaries. Avoid `any`, unnecessary casts, and mock implementations that duplicate the production algorithm.
- Control clocks, timers, identifiers, randomness, and asynchronous completion. Restore mocks and timers after each case.
- Keep each test independent; no reliance on execution order, shared mutable fixtures, or real network access.
- Test cancellation and exception behavior when relevant; do not broadly catch failures or turn them into success-shaped results.

### Angular component and service tests

- Import standalone components into TestBed; follow the affected app's OnPush and signals conventions.
- Set signal inputs with `fixture.componentRef.setInput(...)`, trigger interactions through the rendered DOM where practical, and wait for the appropriate Angular stabilization.
- Use `provideHttpClient()` before `provideHttpClientTesting()` for intercepted HTTP tests. Verify requests and flush controlled responses; call `HttpTestingController.verify()` in cleanup.
- Use router testing utilities or configured routes to assert actual navigation when routing is the behavior under test.
- Test rendered text, visibility, disabled/loading states, emitted outputs, and recovery after errors rather than only checking component existence.
- Preserve the admin test setup. Override its default unauthenticated `AuthService` stub deliberately for authenticated scenarios; use real routing/interceptors when those are the subject of the test.
- Keep Jasmine and Vitest timer/spy APIs separate. Do not copy runner-specific helpers between apps without adapting them.
- For SSR-sensitive code, verify the browser/server boundary and clean up any temporary globals or cookie state.

### API and database integration tests

- Reuse existing integration fixtures. If none exist, confirm a framework and project layout before scaffolding; keep target frameworks and package versions compatible with the API.
- Prefer `WebApplicationFactory<Program>` for in-process API tests, with test-only service/configuration overrides. Do not alter production authentication or middleware to make tests pass.
- Exercise HTTP status codes, response body shape, validation, authorization, and persisted effects. A direct controller call alone is not HTTP integration coverage.
- Use a controlled test-auth handler for endpoint-policy tests. For cookie/session behavior, test the actual relevant auth scheme and cookie flow rather than substituting the boundary being verified.
- Where affected, cover `CookieAuth`, `SessionAuth`, `DynamicAuth` selection, expired sessions, auth-check cookie reissue, and `AdminAccess` policy outcomes.
- For relational behavior, use an explicitly designated disposable database with the relevant provider. EF InMemory is not proof of SQL translation, constraints, transactions, or provider compatibility; SQLite is not proof of SQL Server/MySQL behavior.
- Seed only required synthetic records. Isolate data per test and clean up only resources created by the test. Do not assume transaction rollback covers requests using separate database connections.
- Never apply migrations to a development or production database as a test setup shortcut. Require an explicit test-database target and a safe lifecycle.
- Stub outgoing Anthropic calls and other external services; verify contracts and error paths without sending repository data to third parties.

### Browser UI and end-to-end tests

- Write repeatable test files in the configured runner, not only an interactive browser session.
- Prefer roles and accessible names; use existing stable test IDs when necessary. Avoid brittle CSS chains and positional selectors.
- Use isolated browser contexts and synthetic accounts. Clean up created data and avoid privileged real-user sessions.
- Wait for observable state, responses, or navigation, not fixed sleeps. Use runner-supported asynchronous assertions.
- Distinguish mocked UI tests from full E2E tests. Network stubbing can verify UI states, but does not verify the live API/database integration.
- Cover critical journeys and affected responsive layouts, keyboard navigation, focus management, accessible names, expanded state, and form errors.
- For navigation-menu changes, consider open/close behavior, Escape, focus restoration, route selection, active links, and mobile layout only where supported or required.
- Run automated AXE/WCAG checks when available and supplement them with keyboard/focus checks. Do not claim full WCAG AA compliance from an automated scan alone.
- Configure deterministic visual baselines only when visual testing is requested or already established. Review differences rather than automatically accepting snapshots.
- Keep failure artifacts free of secrets. Store traces/screenshots through the runner's existing ignored-output convention.

## 4. Run focused validation

1. Run the selected existing tests as a baseline where feasible.
2. Add or update tests, then run the smallest selection covering all changed behaviors. Prefer an available test tool or repository task.
3. Use the affected app's Angular CLI test builder, not a bare Vitest invocation that bypasses Angular setup.
4. From the respective frontend directory, typical focused PowerShell commands are:

   Public portal (Karma/Jasmine):

   ```powershell
   npm test -- --watch=false --browsers=ChromeHeadless --include="src/app/components/menu/nav-menu/nav-menu.component.spec.ts"
   ```

   Admin portal (Vitest):

   ```powershell
   npm test -- --watch=false --include="src/app/components/menu/nav-menu/nav-menu.component.spec.ts"
   ```

   Replace the example include with the actual affected specs; check installed CLI help before adding coverage or other runner-specific flags.

5. For .NET, run the actual test project with the platform-appropriate filter selected by the `run-tests` skill. Do not report a successful build or `dotnet test` on a non-test project as passing tests.
6. For browser tests, use the configured project script and start only required services. Verify readiness at the configured URL; do not assume a port is free or stop another user's server.
7. Run affected lint/build checks when changes touch configuration, dependencies, shared fixtures, or production code. Broaden the test selection only when cross-cutting changes or failures warrant it.
8. Restore/install dependencies only after a manifest change or a validation failure identifying missing dependencies. Do not silently install global tooling.
9. Classify failures as new test failures, production defects, existing baseline failures, or environment blockers. Keep failing output visible and resolve in-scope problems without weakening coverage.
10. Review the final diff and remove only temporary artifacts created by this task. Confirm persistent test files are present and unrelated user changes remain intact.

Documentation-only updates to this skill do not require application builds or test execution.

## 5. Completion report

Report concisely:

- Test files created or updated, linked to their locations.
- Behaviors covered and which are unit, integration, mocked UI, or full E2E.
- Exact commands executed, selected test counts, and pass/fail/skip totals when reported by the runner.
- Whether the original regression failure was verified.
- Any untested acceptance criteria, baseline failures, environment blockers, or infrastructure choices awaiting approval.

Never claim that unexecuted tests passed, that a manual browser check is an automated suite, or that a successful run with zero discovered tests validates the change.
