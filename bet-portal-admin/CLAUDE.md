# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
npm install
npm start                                        # dev server at http://localhost:4210
npm run build                                    # SSR production build → dist/bet-portal-admin/
npm test                                         # Karma/Jasmine (Chrome)
ng test --include='**/foo.component.spec.ts'     # single spec
npm run lint                                     # ESLint (TS + HTML)
node dist/bet-portal-admin/server/server.mjs     # serve SSR build
```

To update the `ks-ag-controls` local library: run `install-ks-controls-dev.bat` (dev) or `install-ks-controls-prod.bat` (prod), then `npm install`.

## Architecture

### SSR setup
Angular 20 SSR via `@angular/ssr` + Express (`src/server.ts`). Static assets served from `public/`. API base URL comes from the `API_BASE_URL` injection token set to `environment.apiUrl` (`http://localhost:5026/api` in dev). No i18n — unlike `bet-portal`, this app has no `ngx-translate`.

### Auth
Same auth flow as `bet-portal`: `authGuard` + `authInterceptor` from `ks-ag-controls`. See `bet-portal/CLAUDE.md` for details.

### ag-Grid list pages

List pages use `KsAgGridComponent` (from `ks-ag-controls`) with an `IDatasource` (infinite-row-model / server-side). The reference implementation is `SportsListComponent`.

**Key wiring pattern:**

```
@ViewChild('agGrid') agGridComponent?: KsAgGridComponent<T>
private gridApi?: GridApi       // obtained from IDatasource.getRows params on first call
private gridInitialized = false
private readonly pageIdentifier = 'my-page'  // unique string per page
```

1. **Grid API acquisition** — `gridApi` is captured inside `IDatasource.getRows` on the first call (when `params.api` is available). Use `agGridComponent?.agGrid?.api` as fallback.
2. **`buildQueryFromRequest`** — translates ag-Grid's `filterModel` / `sortModel` to `HttpParams` for the API. Text filters come from `filter.filter`; boolean filters handle string coercion (`'true'`/`'Yes'`/`'1'`).
3. **Inline add** — new rows are pinned to the bottom (`setGridOption('pinnedBottomRowData', [newRow])`), entered into edit mode, then saved via `handleRowSaved` which calls `createXxx` if `id` is absent.
4. **Cell change tracking** — `handleCellChanged` tracks edits in `Map<id|'new', Partial<T>>` so multi-column edits are collected before save.
5. **`ModuleRegistry.registerModules([AllCommunityModule])`** must be called at the top of every list component file.
6. Use `NO_ERRORS_SCHEMA` in the component's `schemas` array to suppress unknown-element errors from `ks-ag-controls` custom elements.

### Grid settings persistence

`GridSettingsService` (`src/app/services/grid-settings.service.ts`) persists column state, filters, and page size per user per page via `GET|POST|DELETE /api/gridsettings/{pageIdentifier}`. It also maintains an in-memory cache (`Map<pageIdentifier, GridSettingsData>`).

**Lifecycle in a list component:**

1. `ngOnInit` → `loadGridSettings()` fetches saved settings; stores them in `loadedSettings`.
2. First `getRows` call → grid is ready → `applyGridSettings()` applies `columnState`, `filterModel`, `pageSize`.
3. `KsAgGridComponent` emits `(settingsSaved)` → `saveGridSettings()` persists; emits `(settingsCleared)` → `deleteSettings()` and resets grid to defaults.

The `gridInitialized` flag handles the race between async settings load and grid readiness: if settings arrive before `getRows`, `applyGridSettings` runs from the settings callback; if `getRows` fires first, `applyGridSettings` runs from `initializeGridSettings`.

### Service pattern

Services define their own request/response interfaces locally (not in a shared contracts folder). All services use `inject()` and `providedIn: 'root'`. Query parameters are built with `HttpParams`.

## Angular Coding Rules

See `.claude/CLAUDE.md` in this directory for the full set. Non-obvious rules:

- Do **not** set `standalone: true` — default in Angular v20+.
- `ChangeDetectionStrategy.OnPush` on every component.
- `input()` / `output()` functions, not `@Input()` / `@Output()` decorators.
- Native control flow (`@if`, `@for`, `@switch`), not structural directives.
- Signals for state; `inject()` instead of constructor injection.
- All routes lazy-loaded via `loadComponent()`.
