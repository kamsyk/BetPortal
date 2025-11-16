import { HttpInterceptorFn } from '@angular/common/http';
import { inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { SsrCookieService } from 'ngx-cookie-service-ssr';

interface GlobalWithRequest {
  __request__?: {
    headers?: {
      cookie?: string;
    };
  };
}

function getCookieOnServer(name: string): string | null {
  //return "fsdfsdsfd";
   try {
     if (typeof window === 'undefined' && typeof global !== 'undefined') {

      const req = (global as GlobalWithRequest).__request__;
      console.log('[getCookieOnServer] Request exists:', !!req);
      console.log('[getCookieOnServer] Headers exist:', !!(req && req.headers));
      console.log('[getCookieOnServer] Cookie header:', req?.headers?.cookie || 'NO COOKIE HEADER');

      return "fsdfsdsfd";
  //     if (req && req.headers && req.headers.cookie) {
  //       return "true";
  //       // const cookies = req.headers.cookie.split(';');
  //       // console.log('[getCookieOnServer] Parsed cookies:', cookies.map(c => c.trim().split('=')[0]));

  //       // for (const cookie of cookies) {
  //       //   const [key, value] = cookie.trim().split('=');
  //       //   if (key === name) {
  //       //     const decoded = decodeURIComponent(value);
  //       //     console.log(`[getCookieOnServer] Found ${name}:`, decoded.substring(0, 10) + '...');
  //       //     return decoded;
  //       //   }
  //       // }
  //     }
     }
  } catch (error) {
    console.error('[getCookieOnServer] Error:', error);
  }
  return null;
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const platformId = inject(PLATFORM_ID);
  const cookieService = inject(SsrCookieService);

  if (isPlatformBrowser(platformId)) {

    // Clone the request to add the 'withCredentials' option
    // This ensures cookies are sent with every HTTP request
    const authReq = req.clone({
      setHeaders: {
        'Content-Type': 'application/json',
      },
      // This is crucial for sending cookies with requests
      withCredentials: true,
    });

    return next(authReq);
  } else {
getCookieOnServer('auth_token');

    console.log('🖥️ [Server] Getting cookies via SsrCookieService');

    const allCookies = cookieService.getAll();
    console.log('🍪 [Server] All cookies found:', Object.keys(allCookies));
    console.log('🍪 [Server] Cookie count:', Object.keys(allCookies).length);

    if (Object.keys(allCookies).length > 0) {
      // ✅ Convert cookies object to cookie header string
      const cookieString = Object.entries(allCookies)
        .map(([name, value]) => `${name}=${value}`) // SsrCookieService handles encoding
        .join('; ');

      console.log('🍪 [Server] Cookie string length:', cookieString.length);
      console.log('🍪 [Server] Cookie string preview:', cookieString.substring(0, 100) + '...');

      // Look for specific auth tokens
      const authToken = allCookies['auth_token'] ||
                       allCookies['AuthCookie'] ||
                       allCookies['jwt_token'];

      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
        'Cookie': cookieString // ✅ This is the key - send as string, not object
      };

      // If we found an auth token, also add it as Authorization header
      if (authToken) {
        console.log('✅ [Server] Auth token found, adding Authorization header');
        headers['Authorization'] = `Bearer ${authToken}`;
        headers['X-Auth-Token'] = authToken;
      } else {
        console.log('❌ [Server] No auth token found in cookies');
      }

      const authReq = req.clone({
        setHeaders: headers,
        withCredentials: true,
      });

      return next(authReq);
    } else {
      console.log('❌ [Server] No cookies found');

      // No cookies available
      const authReq = req.clone({
        setHeaders: {
          'Content-Type': 'application/json'
        },
        withCredentials: true,
      });

      return next(authReq);
    }
   // const allCookies = cookieService.getAll();

    // const authReq = req.clone({
    //       setHeaders: {
    //         // 'Content-Type': 'application/json',
    //         // 'Authorization': `Bearer ${token}`,
    //         // 'X-Auth-Token': token,
    //         'Cookie': allCookies // Forward all cookies
    //       },
    //       withCredentials: true,
    //     });
    //     return next(authReq);

    /*const token = cookieService.get('auth_token');
    console.log('[SSR Guard] Token from SsrCookieService:', token ? 'EXISTS' : 'NULL');
    const allCookies = cookieService.getAll();

  if (token) {
        console.log('✅ [Server] Token found, adding to headers');
        const authReq = req.clone({
          setHeaders: {
            'Content-Type': 'application/json',
            'Authorization': `Bearer ${token}`,
            'X-Auth-Token': token,
            'Cookie': cookies // Forward all cookies
          },
          withCredentials: true,
        });
        return next(authReq);
      } else {
        console.log('❌ [Server] No auth token found in cookies');
        // Still forward cookies even if no auth token
        const authReq = req.clone({
          setHeaders: {
            'Content-Type': 'application/json',
            'Cookie': cookies
          },
          withCredentials: true,
        });
        return next(authReq);


    // console.log('🖥️ [Server] Using request headers');

    // const request = inject(REQUEST, { optional: true });

    // if (request && request.headers.cookie) {
    //   const cookies = request.headers.cookie;
    //   console.log('🍪 [Server] Raw cookies:', cookies);

    //   const authToken = extractCookieFromHeader(cookies, 'auth_token') ||
    //                    extractCookieFromHeader(cookies, 'AuthCookie') ||
    //                    extractCookieFromHeader(cookies, 'jwt_token');

    //   if (authToken) {
    //     console.log('✅ [Server] Token found, adding to headers');
    //     const authReq = req.clone({
    //       setHeaders: {
    //         'Content-Type': 'application/json',
    //         'Authorization': `Bearer ${authToken}`,
    //         'X-Auth-Token': authToken,
    //         'Cookie': cookies // Forward all cookies
    //       }
    //     });
    //     return next(authReq);
    //   } else {
    //     console.log('❌ [Server] No auth token found in cookies');
    //     // Still forward cookies even if no auth token
    //     const authReq = req.clone({
    //       setHeaders: {
    //         'Content-Type': 'application/json',
    //         'Cookie': cookies
    //       }
    //     });
    //     return next(authReq);
    //   }
    // } else {
    //   console.log('❌ [Server] No request or cookies available');
    // }
*/
  }
  return next(req);

};
