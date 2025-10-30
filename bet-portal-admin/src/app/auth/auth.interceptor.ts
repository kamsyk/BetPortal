import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError } from 'rxjs/operators';
import { throwError } from 'rxjs';
import { AuthService } from 'ks-ag-controls';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // const auth = inject(AuthService);
  // //const token = auth.getToken();
  // const token = auth.validateSession();


  // if (token) {
  //   req = req.clone({ setHeaders: { Authorization: `Bearer ${token}` } });
  // }

  // return next(req).pipe(
  //   catchError((err) => {
  //     if (err?.status === 401) {
  //       auth.logout();
  //     }
  //     return throwError(() => err);
  //   }),
  // );

  const credentialsReq = req.clone({
    withCredentials: true, // 🔥 This includes cookies with every request
    setHeaders: {
      'Content-Type': 'application/json'
    }
  });

  console.log('[Credentials Interceptor] Request to:', req.url, 'with credentials');

  return next(credentialsReq);
};
