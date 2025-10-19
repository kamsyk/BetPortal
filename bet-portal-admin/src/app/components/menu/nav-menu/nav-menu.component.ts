import { Component, inject } from '@angular/core';
import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { KsSignUpComponent } from 'ks-ag-controls';

@Component({
  selector: 'app-nav-menu',
  standalone: true,
  imports: [MatDialogModule, MatInputModule],
  templateUrl: './nav-menu.component.html',
  styleUrls: ['./nav-menu.component.scss'],
})
export class NavMenuComponent {
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

  signUp() {
    alert('test');
    this.dialog.open(KsSignUpComponent);
  }
}
