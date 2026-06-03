import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

@Injectable({
  providedIn: 'root',
})
export class ThemeService {
  private readonly THEME_KEY = 'app-theme';
  private platformId = inject(PLATFORM_ID);
  private isBrowser = isPlatformBrowser(this.platformId);

  isDarkMode = signal<boolean>(false);

  constructor() {
    // Only access browser APIs if in browser environment
    if (this.isBrowser) {
      // Load theme preference from localStorage
      const savedTheme = localStorage.getItem(this.THEME_KEY);

      if (savedTheme) {
        this.isDarkMode.set(savedTheme === 'dark');
      } else {
        // Check system preference
        const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
        this.isDarkMode.set(prefersDark);
      }

      // Apply theme on initialization
      this.applyTheme();

      // Watch for changes and apply theme
      effect(() => {
        this.applyTheme();
      });
    }
  }

  toggleTheme(): void {
    this.isDarkMode.update(value => !value);
  }

  private applyTheme(): void {
    if (!this.isBrowser) {
      return;
    }

    const theme = this.isDarkMode() ? 'dark' : 'light';

    // Update document class
    if (this.isDarkMode()) {
      document.body.classList.add('dark-theme');
      document.body.classList.remove('light-theme');
    } else {
      document.body.classList.add('light-theme');
      document.body.classList.remove('dark-theme');
    }

    // Save preference
    localStorage.setItem(this.THEME_KEY, theme);
  }
}
