import { Component, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { AuthService } from 'ks-ag-controls';
import { Router } from '@angular/router';

@Component({
  selector: 'app-nav-menu',
  standalone: true,
  imports: [MatDialogModule, MatInputModule],
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss'],
})
export class NavMenuComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  isMobile = false;

  private breakpointObserver = inject(BreakpointObserver);
  private dialog = inject(MatDialog);

  constructor() {
    this.breakpointObserver
      .observe([Breakpoints.Handset])
      .subscribe((result) => {
        this.isMobile = result.matches;
      });
  }

  logOut() {
    this.authService.logout();
    this.router.navigateByUrl('/login');
  }
}
