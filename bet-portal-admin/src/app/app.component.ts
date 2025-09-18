import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { NavMenuComponent } from './components/menu/nav-menu/nav-menu.component';

@Component({
  selector: 'app-root',
  imports: [
    RouterOutlet,
    MatInputModule,
    FormsModule,
    NavMenuComponent,
  ],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'bet-portal-admin';

}
