import { Component, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { Router, RouterLink } from '@angular/router';
import { MatMenuModule } from '@angular/material/menu';
import { MatButtonModule } from '@angular/material/button';
import { MatToolbarModule } from '@angular/material/toolbar';
import { AuthService } from 'ks-ag-controls';

@Component({
  selector: 'app-nav-menu',
  standalone: true,
  imports: [RouterLink, MatMenuModule, MatButtonModule, MatToolbarModule],
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss'],
})
export class NavMenuComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

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
}
