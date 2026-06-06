import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatInputModule } from '@angular/material/input';
import { NavMenuComponent } from './components/menu/nav-menu/nav-menu.component';
import { ThemeService } from './services/theme.service';
import { LanguageService } from './services/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MatInputModule, FormsModule, NavMenuComponent],
  templateUrl: './app.component.html',
  styleUrl: './app.component.scss',
})
export class AppComponent {
  title = 'bet-portal';

  private themeService = inject(ThemeService);
  private languageService = inject(LanguageService);

  constructor() {
    this.languageService.init();
  }
}
