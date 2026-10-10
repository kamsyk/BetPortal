import { afterNextRender, ChangeDetectionStrategy, Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { NavMenuComponent } from './components/menu/nav-menu/nav-menu.component';
import { ThemeService } from './services/theme.service';
import { LanguageService } from './services/language.service';
import { AuthService } from 'ks-ag-controls';

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
  private destroyRef = inject(DestroyRef);

  constructor() {
    this.languageService.init();
    afterNextRender(() => {
      this.authService.checkAuthStatus()
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe();
    });
  }
}
