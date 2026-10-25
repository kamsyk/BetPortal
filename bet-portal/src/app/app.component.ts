import { afterNextRender, ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { NavMenuComponent } from './components/menu/nav-menu/nav-menu.component';
import { ThemeService } from './services/theme.service';
import { LanguageService } from './services/language.service';
import { AuthService } from 'ks-ag-controls';
import { AuthDialogService } from './services/auth-dialog.service';

@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterOutlet, MatInputModule, FormsModule, NavMenuComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'bet-portal';

  private themeService = inject(ThemeService);
  private languageService = inject(LanguageService);
  private authService = inject(AuthService);
  private authDialog = inject(AuthDialogService);
  private route = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  constructor() {
    this.languageService.init();
    afterNextRender(() => {
      this.authService.checkAuthStatus()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe();
      this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
        const dialog = params.get('dialog');
        if (dialog !== 'login' && dialog !== 'sign-up') {
          return;
        }

        this.authDialog.openRequested(dialog, {
          returnUrl: params.get('returnUrl'),
          googleError: params.get('googleError'),
          facebookError: params.get('facebookError'),
          microsoftError: params.get('microsoftError'),
          errorId: params.get('errorId'),
          errorDetails: params.get('errorDetails'),
          signUp: params.get('signUp') === 'true',
        });
      });
    });
  }
}
