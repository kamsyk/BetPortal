import { provideServerRendering, withRoutes } from '@angular/ssr';
//import { REQUEST } from '@angular/ssr/tokens';
import { mergeApplicationConfig, ApplicationConfig, REQUEST } from '@angular/core';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { SsrCookieService } from 'ngx-cookie-service-ssr';
import { AUTH_SERVER_TOKEN } from '../core/tokens';

// const serverConfig: ApplicationConfig = {
//   providers: [provideServerRendering(withRoutes(serverRoutes))],
// };

const serverConfig: ApplicationConfig = {
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    SsrCookieService, // Ensure this is provided
    {
      provide: AUTH_SERVER_TOKEN,
      deps: [REQUEST],
      useFactory: (req: Request) => {
        const cookie = req.headers.get('cookie') ?? '';
        console.log('[SSR Config] Raw cookie header:', cookie);
        const m = /auth_token=([^;]+)/i.exec(cookie);
        const token = m ? decodeURIComponent(m[1]) : null;
        console.log('[SSR Config] Parsed auth_token:', token ? 'EXISTS' : 'NULL');
        return token;
      },
    },
  ],
};

// const serverConfig: ApplicationConfig = {
//   providers: [
//     provideServerRendering(withRoutes(serverRoutes)),
//     SsrCookieService,
//   ],
// };

export const config = mergeApplicationConfig(appConfig, serverConfig);
