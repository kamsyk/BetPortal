import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { afterNextRender, ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { API_BASE_URL } from '../../../core/tokens';
import { AuthDialogService } from '../../services/auth-dialog.service';

@Component({
  selector: 'app-user-token',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe],
  template: `
    <main aria-live="polite" [attr.aria-busy]="pending()">
      <h1>{{ 'userToken.title' | translate }}</h1>
      @if (pending()) {
        <p role="status">{{ 'userToken.pending' | translate }}</p>
      } @else if (errorKey()) {
        <p role="alert">{{ errorKey() | translate }}</p>
        <button type="button" (click)="signIn()">{{ 'nav.signIn' | translate }}</button>
      }
    </main>
  `,
})
export class UserTokenComponent {
  private readonly http = inject(HttpClient);
  private readonly apiBase = inject(API_BASE_URL);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly authDialog = inject(AuthDialogService);
  signIn(): void {
    this.authDialog.openLogin();
  }

  readonly pending = signal(true);
  readonly errorKey = signal('');

  constructor() {
    afterNextRender(() => {
      const token = this.route.snapshot.queryParamMap.get('token');
      if (!token?.trim() || token.length > 100) {
        this.pending.set(false);
        this.errorKey.set('userToken.invalid');
        return;
      }

      this.http.post<void>(`${this.apiBase}/user-token`, { token })
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: () => {
            void this.router.navigateByUrl('/welcome', { replaceUrl: true });
          },
          error: (error: HttpErrorResponse) => {
            this.pending.set(false);
            this.errorKey.set(error.status === 400 || error.status === 404
              ? 'userToken.invalid'
              : 'userToken.failed');
          },
        });
    });
  }
}
