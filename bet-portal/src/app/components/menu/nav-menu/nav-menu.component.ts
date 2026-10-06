import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from 'ks-ag-controls';
import { ThemeService } from '../../../services/theme.service';
import { LanguageService } from '../../../services/language.service';

@Component({
  selector: 'app-nav-menu',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    RouterLink,
    RouterLinkActive,
    MatMenuModule,
    MatButtonModule,
    MatToolbarModule,
    MatIconModule,
    TranslatePipe,
  ],
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss'],
})
export class NavMenuComponent {
  private authService = inject(AuthService);
  private router = inject(Router);
  themeService = inject(ThemeService);
  languageService = inject(LanguageService);

  isLoggedIn(): boolean {
    return this.authService.isAuthenticated();
  }

  logOut() {
    this.authService.logout().subscribe({
      next: (response) => {
        console.log('Logout response:', response);
        this.authService.logout();
        this.router.navigateByUrl('/login');
      },
    });
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
