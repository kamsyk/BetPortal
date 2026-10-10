# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
npm install
npm start                                        # dev server at http://localhost:4200
npm run build                                    # SSR production build → dist/bet-portal/
npm test -- --watch=false                         # Vitest/jsdom via Angular unit-test builder
ng test --include='**/foo.component.spec.ts'     # single spec
npm run lint                                     # ESLint (TS + HTML)
node dist/bet-portal/server/server.mjs           # serve SSR build
```

To update the `ks-ag-controls` local library: run `install-ks-controls-dev.bat` (dev) or `install-ks-controls-prod.bat` (prod), then `npm install`.

## Architecture

Activation links use `/user-token?token=<token>`. The lazy public page posts the token to `/api/user-token` after the first browser render (never during SSR/prerender), then redirects to `/welcome` with history replacement so the token is removed from the current URL. Invalid/used tokens or server errors are shown inline. The welcome page confirms activation and links to Login; activation does not automatically sign in the user.

The Sign up dialog uses `/api/auth/google/register`, separately from Google's login endpoint. Verified Google registration creates an inactive user plus an initialization token and redirects to the public `/sign-up-confirmation` page. Registration does not sign the user in or send an activation email. Duplicate emails return to Login with an account-already-exists message.

### SSR setup
Angular 21 app with SSR via `@angular/ssr` + Express (`src/server.ts`). The SSR entry is `src/main.server.ts`; the browser entry is `src/main.ts`. Static assets (including i18n JSON files) are served from `public/`.

### API communication
All HTTP calls include `withCredentials: true` (set by `authInterceptor`). On the server side, the interceptor forwards the raw `Cookie` header from the incoming request via `global.__request__` so SSR can make authenticated API calls. The API base URL is provided via the `API_BASE_URL` injection token (`src/core/tokens.ts`), set from `environment.apiUrl` in `app.config.ts`.

### Auth flow
The home route (`/`) and login route are public. The advisor route (`/advisor`)
requires authentication via `authGuard`.

The public home page introduces portal data, the AI Advisor, language selection,
and sign-in. Its content is translated in all three languages and scrolls within
its own viewport because the application shell disables body scrolling.

`authGuard` checks `AuthService.isAuthenticated()` (sync, from `ks-ag-controls`) first. If not locally authenticated it calls `GET /api/auth/check` and redirects to `/login` on failure. `AuthService` is external — it lives in the `ks-ag-controls` package, not in this repo.

`AppComponent` subscribes to `AuthService.checkAuthStatus()` after the first browser render to restore cookie-backed authentication on public pages, including a full-page redirect from Google sign-in. This updates the menu's auth signal without visiting Login. The startup check does not run during SSR or prerendering.

### Internationalisation
Translations are in `public/i18n/{en,cs,de}.json`, loaded at runtime by `ngx-translate` from `/i18n/<lang>.json`. `LanguageService` (`src/app/services/language.service.ts`) is the single source of truth: it initialises on app start (called from `AppComponent` constructor), reads from localStorage (`bet-portal.lang`), falls back to browser language then `'en'`. Supported languages: `en`, `cs`, `de`. Use `TranslatePipe` in templates and `TranslateService.instant()` for error strings in components.

The login page binds `LanguageService.currentLanguage()` to the local library's
`lib-ks-login` language input, so login labels update when the selected language changes.

Google sign-in requires an existing active portal account with the same verified email. Unknown accounts return to `/login?googleError=account-required&returnUrl=...`, where a translated account-required alert is shown and the original destination is preserved. After the browser renders, the login page calls the library's `openSignUp()` to open the same registration dialog as the Sign up button, once per page load. Google sign-in does not register users automatically.

### Navigation
The user-profile icon opens a shared desktop/mobile menu containing a nested language selector and either Sign in for anonymous users or Logout for authenticated users. Language selection remains available to guests. Main navigation and theme controls are shown only when authenticated; the mobile navigation menu does not contain language or logout actions.

### AI Advisor page (`/advisor`)
`AdvisorComponent` maintains conversation state in a `signal<ChatMessage[]>`. `ChatService.sendMessage()` sends the last 10 messages as context to `POST /api/chat/ask`. The component uses `@ViewChild` + `AfterViewChecked` to auto-scroll — the `shouldScrollToBottom` flag avoids scroll-on-every-change-detection cycle.

### Global styles
Bootstrap 5 CSS is imported in `src/styles.scss`; Bootstrap JS bundle is loaded as a script via `angular.json`. Angular Material theme is also applied globally. Component styles use `.scss`.

## Angular Coding Rules

See `.claude/CLAUDE.md` in this directory for the full set. Non-obvious rules specific to this project:

- Do **not** set `standalone: true` — default in Angular v20+.
- `ChangeDetectionStrategy.OnPush` on every component.
- `input()` / `output()` functions, not `@Input()` / `@Output()` decorators.
- Native control flow (`@if`, `@for`, `@switch`), not structural directives.
- Signals for state; `inject()` instead of constructor injection.
- All routes lazy-loaded via `loadComponent()`.
