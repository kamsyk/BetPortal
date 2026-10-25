import { isPlatformBrowser } from '@angular/common';
import { Injectable, PLATFORM_ID, inject, type Type } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { MatDialog, type MatDialogRef } from '@angular/material/dialog';

export interface AuthDialogRequest {
  returnUrl?: string | null;
  googleError?: string | null;
  facebookError?: string | null;
  microsoftError?: string | null;
  errorId?: string | null;
  signUp?: boolean;
}

@Injectable({ providedIn: 'root' })
export class AuthDialogService {
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private ref: MatDialogRef<unknown> | null = null;
  private openKind: 'login' | 'sign-up' | null = null;
  private requestId = 0;

  openLogin(request: AuthDialogRequest = {}): void {
    void this.open('login', request);
  }

  openSignUp(request: AuthDialogRequest = {}): void {
    void this.open('sign-up', request);
  }

  openRequested(kind: 'login' | 'sign-up', request: AuthDialogRequest = {}): void {
    const showSignUp = kind === 'sign-up'
      || request.signUp
      || request.googleError === 'account-required'
      || request.facebookError === 'account-required'
      || request.microsoftError === 'account-required';
    if (showSignUp) {
      this.openSignUp(request);
      return;
    }

    this.openLogin(request);
  }

  private async open(kind: 'login' | 'sign-up', request: AuthDialogRequest): Promise<void> {
    if (!this.isBrowser || (this.openKind === kind && this.ref)) {
      return;
    }

    const requestId = ++this.requestId;
    const component = kind === 'login'
      ? (await import('../pages/login/login.component')).LoginComponent
      : (await import('../pages/sign-up/sign-up.component')).SignUpComponent;
    if (requestId !== this.requestId) {
      return;
    }

    const previous = this.ref;
    const ref = this.dialog.open(component as Type<unknown>, {
      data: request,
      width: kind === 'login' ? '468px' : '560px',
      maxWidth: 'calc(100vw - 32px)',
      maxHeight: 'calc(100vh - 32px)',
      autoFocus: false,
      restoreFocus: true,
      ariaModal: true,
      closeOnNavigation: true,
      panelClass: 'login-dialog-panel',
    });
    this.ref = ref;
    this.openKind = kind;
    previous?.close();
    ref.afterClosed().subscribe(() => {
      if (this.ref !== ref) {
        return;
      }

      this.ref = null;
      this.openKind = null;
      this.clearDialogQuery();
    });
  }

  private clearDialogQuery(): void {
    const params = this.route.snapshot.queryParamMap;
    if (!params.has('dialog') && !params.has('googleError') && !params.has('facebookError')
      && !params.has('microsoftError') && !params.has('errorId') && !params.has('signUp')) {
      return;
    }

    void this.router.navigate([], {
      queryParams: {
        dialog: null,
        googleError: null,
        facebookError: null,
        microsoftError: null,
        errorId: null,
        signUp: null,
        returnUrl: null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  }
}
