# BetPortalAdmin

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.15.

## Local controls dependency

This portal uses Angular 21 and the local `ks-ag-controls-0.0.7.tgz` package
referenced by [package.json](./package.json). Use Node.js 22.12 or newer in the
22.x release line (Node.js 22.19 is supported).

To build, package, and install the library from the sibling
`Angular\ks-ag-controls\ks-ag-controls-workspace` checkout, run from this directory:

```powershell
.\scripts\install-ks-controls-dev.bat
```

The library checkout must have its dependencies installed first. The script
builds the library styles and Angular package, stops on errors, and installs
without first removing the existing dependency. Once the tarball exists,
`npm ci` can also restore the portal dependencies.

Do not use `--force` or `--legacy-peer-deps` to install this library on Angular 20:
version 0.0.7 requires Angular 21. The published npm version 0.0.6 does not export
`AuthService` or `KsLoginComponent`; use the local package rather than the
production install script for this checkout.

## Development server

To start a local development server, run:

```bash
ng serve
```

Once the server is running, open your browser and navigate to `http://localhost:4200/`. The application will automatically reload whenever you modify any of the source files.

## Code scaffolding

Angular CLI includes powerful code scaffolding tools. To generate a new component, run:

```bash
ng generate component component-name
```

For a complete list of available schematics (such as `components`, `directives`, or `pipes`), run:

```bash
ng generate --help
```

## Building

To build the project run:

```bash
ng build
```

This will compile your project and store the build artifacts in the `dist/` directory. By default, the production build optimizes your application for performance and speed.

## Running unit tests

Unit tests use Angular's `@angular/build:unit-test` builder with Vitest and jsdom.
Run them once with:

```powershell
npm test -- --watch=false
```

Run a focused spec with `npm test -- --watch=false --include="src/app/components/menu/nav-menu/nav-menu.component.spec.ts"`.
Browser UI coverage still uses Playwright as described below.

Karma was removed because its dependency chain includes the unpatched `braces`
stack-exhaustion denial-of-service advisory (GHSA-vfj7-8cjw-p6xm). Do not restore
Karma or use forced downgrades to address this advisory. Check all installed
dependencies with `npm audit` and production dependencies with
`npm audit --omit=dev`.

If npm 10 fails with `Cannot read properties of null (reading 'edgesOut')`
while resolving Vitest's optional peers, use npm 11 for dependency changes:
`npm exec --yes --package=npm@11 -- npm install`. Restoring the checked-in
lockfile with `npm ci` also works with npm 10.

## Running navigation-menu browser tests

The navigation-menu browser suite uses Playwright Test with Chromium at desktop
and mobile viewports. It builds a static browser-only bundle and serves it on
`http://127.0.0.1:4300`, avoiding server-side rendering and prerender-time API
requests. It fails rather than reusing an existing server on that port.
Browser-side API requests are stubbed with synthetic anonymous or authenticated
responses; the suite does not require a running API or real user accounts.

```powershell
npm run test:e2e
npm run test:e2e -- --project=mobile-chromium
```

The suite is mocked UI coverage, not full API or database E2E coverage. Reports
and failure artifacts are written to ignored `playwright-report/` and
`test-results/` directories.

## Additional Resources

For more information on using the Angular CLI, including detailed command references, visit the [Angular CLI Overview and Command Reference](https://angular.dev/tools/cli) page.
