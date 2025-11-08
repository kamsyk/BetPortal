import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Clone the request to add the 'withCredentials' option
  // This ensures cookies are sent with every HTTP request
  const authReq = req.clone({
    setHeaders: {
      'Content-Type': 'application/json'
    },
    // This is crucial for sending cookies with requests
    withCredentials: true
  });

  return next(authReq);
};
