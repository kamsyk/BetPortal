# BetPortalAdmin

This project was generated using [Angular CLI](https://github.com/angular/angular-cli) version 19.2.15.

## Local controls dependency

Before running `npm ci`, ensure the local `ks-ag-controls-0.0.6.tgz` package
referenced by [package.json](./package.json) exists. Use the Angular 20-compatible
library source at commit `ab090d0` in the sibling `ks-ag-controls-workspace`
repository. In a separate checkout of that commit, run:

```powershell
npm ci
npm run build:lib:styles
npx ng build ks-ag-controls --configuration production
Set-Location .\dist\ks-ag-controls
npm pack
```

Copy the resulting tarball to the local path referenced by the dependency.
The published npm version 0.0.6 does not export `AuthService` or
`KsLoginComponent`; local version 0.0.7 requires Angular 21 and is not compatible
with this Angular 20 portal.

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

To execute unit tests with the [Karma](https://karma-runner.github.io) test runner, use the following command:

```bash
ng test
```

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
