import { provideServerRendering, withRoutes } from '@angular/ssr';
import { mergeApplicationConfig, ApplicationConfig, REQUEST } from '@angular/core';
import { appConfig } from './app.config';
import { serverRoutes } from './app.routes.server';
import { AUTH_SERVER_TOKEN } from '../core/tokens';

const serverConfig: ApplicationConfig = {
  //providers: [provideServerRendering(withRoutes(serverRoutes))],
  providers: [
    provideServerRendering(withRoutes(serverRoutes)),
    {
      provide: AUTH_SERVER_TOKEN,
      deps: [REQUEST],
      useFactory: (req: Request) => {
        const cookie = req.headers.get('cookie') ?? '';
        const m = /auth_token=([^;]+)/i.exec(cookie);
        return m ? decodeURIComponent(m[1]) : null;
      },
    },
  ],
};

export const config = mergeApplicationConfig(appConfig, serverConfig);
