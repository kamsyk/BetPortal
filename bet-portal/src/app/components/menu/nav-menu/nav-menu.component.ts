import { Component, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Router, RouterLink } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatIconModule } from '@angular/material/icon';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthService } from 'ks-ag-controls';
import { ThemeService } from '../../../services/theme.service';
import { LanguageService } from '../../../services/language.service';

@Component({
  selector: 'app-nav-menu',
  standalone: true,
  imports: [
    RouterLink,
    MatMenuModule,
    MatButtonModule,
    MatToolbarModule,
    MatIconModule,
    MatSelectModule,
    MatFormFieldModule,
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

  isMobile = false;

  private breakpointObserver = inject(BreakpointObserver);

  constructor() {
    this.breakpointObserver
      .observe([Breakpoints.Handset])
      .subscribe((result) => {
        this.isMobile = result.matches;
      });
  }

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
