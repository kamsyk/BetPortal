import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Title } from '@angular/platform-browser';
import { TranslateService } from '@ngx-translate/core';

export type AppLanguage = 'en' | 'cs' | 'de';

const STORAGE_KEY = 'bet-portal.lang';

export const SUPPORTED_LANGUAGES: AppLanguage[] = ['en', 'cs', 'de'];
export const DEFAULT_LANGUAGE: AppLanguage = 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);
  private readonly title = inject(Title);
  private readonly platformId = inject(PLATFORM_ID);

  readonly languages = SUPPORTED_LANGUAGES;
  readonly currentLanguage = signal<AppLanguage>(DEFAULT_LANGUAGE);

  init(): void {
    this.translate.addLangs(this.languages);
    this.translate.setFallbackLang(DEFAULT_LANGUAGE);

    const stored = this.getStoredLanguage();
    const browserLang = this.mapBrowserLanguage(
      this.translate.getBrowserLang(),
    );
    const language = stored ?? browserLang ?? DEFAULT_LANGUAGE;

    this.setLanguage(language);
  }

  setLanguage(language: AppLanguage): void {
    this.translate.use(language).subscribe(() => {
      this.currentLanguage.set(language);
      this.storeLanguage(language);
      this.updateDocumentLanguage(language);
      this.updateTitle();
    });
  }

  private mapBrowserLanguage(lang?: string): AppLanguage | null {
    if (!lang) {
      return null;
    }

    if (lang === 'cs' || lang.startsWith('cs')) {
      return 'cs';
    }

    if (lang === 'de' || lang.startsWith('de')) {
      return 'de';
    }

    if (lang === 'en' || lang.startsWith('en')) {
      return 'en';
    }

    return null;
  }

  private getStoredLanguage(): AppLanguage | null {
    if (!isPlatformBrowser(this.platformId)) {
      return null;
    }

    const stored = localStorage.getItem(STORAGE_KEY) as AppLanguage | null;
    return stored && this.languages.includes(stored) ? stored : null;
  }

  private storeLanguage(language: AppLanguage): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    localStorage.setItem(STORAGE_KEY, language);
  }

  private updateDocumentLanguage(language: AppLanguage): void {
    if (!isPlatformBrowser(this.platformId)) {
      return;
    }

    document.documentElement.lang = language;
  }

  private updateTitle(): void {
    this.translate.get('app.title').subscribe((title) => {
      this.title.setTitle(title);
    });
  }
}
