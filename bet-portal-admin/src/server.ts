import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import express from 'express';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';


const serverDistFolder = dirname(fileURLToPath(import.meta.url));
const browserDistFolder = resolve(serverDistFolder, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine();

interface GlobalWithRequest {
  __request__?: express.Request;
}

/**
 * Example Express Rest API endpoints can be defined here.
 * Uncomment and define endpoints as necessary.
 *
 * Example:
 * ```ts
 * app.get('/api/**', (req, res) => {
 *   // Handle API request
 * });
 * ```
 */

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

//TODO: KSY Fix global request usage in auth.guard.ts

/**
 * Handle all other requests by rendering the Angular application.
 */
// app.use((req, res, next) => {
//   angularApp
//     .handle(req)
//     .then((response) =>
//       response ? writeResponseToNodeResponse(response, res) : next(),
//     )
//     .catch(next);
// });

app.use((req, res, next) => {
  console.log(`[Express] ${req.method} ${req.url}`);
  console.log('[Express] Cookie header:', req.headers.cookie || 'NO COOKIES');

  // Store request in global for Angular guards to access
 (globalThis as unknown as GlobalWithRequest).__request__ = req;

  angularApp
    .handle(req)
    .then((response) => {
      // Clear the global request after handling
      delete (globalThis as unknown as GlobalWithRequest).__request__;
      return response ? writeResponseToNodeResponse(response, res) : next();
    })
    .catch((error) => {
      // Clear the global request on error too
      delete (globalThis as unknown as GlobalWithRequest).__request__;
      next(error);
    });
});

/**
 * Start the server if this module is the main entry point.
 * The server listens on the port defined by the `PORT` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url)) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, () => {
    console.log(`Node Express server listening on http://localhost:${port}`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
