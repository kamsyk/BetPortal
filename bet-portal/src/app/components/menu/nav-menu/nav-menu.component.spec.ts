import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter, Router } from '@angular/router';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { OverlayContainer } from '@angular/cdk/overlay';
import { AuthService } from 'ks-ag-controls';
import { provideTranslateService, TranslateLoader, provideTranslateLoader, } from '@ngx-translate/core';
import { Observable, of, Subject } from 'rxjs';

import { NavMenuComponent } from './nav-menu.component';
import { AuthDialogService } from '../../../services/auth-dialog.service';
import { AppLanguage, LanguageService } from '../../../services/language.service';
import { ThemeService } from '../../../services/theme.service';

class FakeTranslateLoader implements TranslateLoader {
    getTranslation(): Observable<Record<string, string>> {
        return of({});
    }
}

describe('NavMenuComponent', () => {
    let component: NavMenuComponent;
    let fixture: ComponentFixture<NavMenuComponent>;
    const authenticated = signal(false);
    const darkMode = signal(false);
    const currentLanguage = signal('en');
    const toggleTheme = vi.fn<ThemeService['toggleTheme']>();
    const setLanguage = vi.fn<(language: AppLanguage) => void>();
    const logout = vi.fn<AuthService['logout']>();
    const openLogin = vi.fn<AuthDialogService['openLogin']>();
    const openSignUp = vi.fn<AuthDialogService['openSignUp']>();
    let logoutResponse: Subject<{
        success: boolean;
        message: string;
    }>;
    let overlayContainer: OverlayContainer;

    async function settle(): Promise<void> {
        fixture.detectChanges();
        await fixture.whenStable();
        fixture.detectChanges();
    }

    function button(selector: string): HTMLButtonElement {
        const element = (fixture.nativeElement as HTMLElement)
            .querySelector<HTMLButtonElement>(selector)
            ?? overlayContainer.getContainerElement()
                .querySelector<HTMLButtonElement>(selector);
        if (!element) {
            throw new Error(`Missing menu control: ${selector}`);
        }
        return element;
    }

    function menu(): HTMLElement {
        const panels = overlayContainer.getContainerElement()
            .querySelectorAll<HTMLElement>('[role="menu"]');
        const element = panels.item(panels.length - 1);
        if (!element) {
            throw new Error('Expected an open menu');
        }
        return element;
    }

    async function openMenu(selector: string): Promise<HTMLElement> {
        button(selector).click();
        await settle();
        return menu();
    }

    function menuButton(label: string): HTMLButtonElement {
        const element = Array.from(menu().querySelectorAll<HTMLButtonElement>('button'))
            .find(item => item.textContent?.includes(label));
        if (!element) {
            throw new Error(`Missing menu item: ${label}`);
        }
        return element;
    }

    beforeEach(async () => {
        authenticated.set(false);
        darkMode.set(false);
        currentLanguage.set('en');
        toggleTheme.mockClear();
        toggleTheme.mockImplementation(() => darkMode.update(value => !value));
        setLanguage.mockClear();
        setLanguage.mockImplementation(language => currentLanguage.set(language));
        logout.mockClear();
        openLogin.mockClear();
        openSignUp.mockClear();
        logoutResponse = new Subject();
        logout.mockReturnValue(logoutResponse);
        await TestBed.configureTestingModule({
            imports: [NavMenuComponent],
            providers: [
                provideRouter([
                    { path: '', component: NavMenuComponent },
                    { path: 'advisor', component: NavMenuComponent },
                    { path: 'login', component: NavMenuComponent },
                    { path: 'sign-up', component: NavMenuComponent },
                ]),
                provideNoopAnimations(),
                { provide: AuthService, useValue: { isAuthenticated: authenticated, logout } },
                { provide: AuthDialogService, useValue: { openLogin, openSignUp } },
                {
                    provide: ThemeService,
                    useValue: { isDarkMode: darkMode, toggleTheme },
                },
                {
                    provide: LanguageService,
                    useValue: {
                        languages: ['en', 'cs', 'de'],
                        currentLanguage,
                        setLanguage,
                    },
                },
                provideTranslateService({
                    loader: provideTranslateLoader(FakeTranslateLoader),
                }),
            ],
        }).compileComponents();

        fixture = TestBed.createComponent(NavMenuComponent);
        component = fixture.componentInstance;
        overlayContainer = TestBed.inject(OverlayContainer);
        fixture.detectChanges();
    });

    afterEach(() => {
        logoutResponse.complete();
        fixture.destroy();
    });

    it('should create', () => {
        expect(component).toBeTruthy();
    });

    it('shows anonymous toolbar actions without a user profile icon', () => {
        const element: HTMLElement = fixture.nativeElement;
        expect(element.querySelector('.primary-nav')).toBeNull();
        expect(element.querySelector('.profile-button')).toBeNull();
        expect(element.querySelector('.sign-in-link')?.tagName).toBe('BUTTON');
        expect(element.querySelector('.sign-up-link')?.tagName).toBe('BUTTON');
        expect(element.querySelector('.desktop-actions .language-button')).not.toBeNull();
        expect(element.querySelector('.theme-button')).not.toBeNull();
        expect(element.querySelector('.mobile-menu-button')).not.toBeNull();
        expect(element.querySelector('.logout-button')).toBeNull();
    });

    it('marks only the current page as active', async () => {
        authenticated.set(true);
        fixture.detectChanges();
        await TestBed.inject(Router).navigateByUrl('/advisor');
        await fixture.whenStable();
        fixture.detectChanges();

        const element: HTMLElement = fixture.nativeElement;
        const activeLinks = element.querySelectorAll('.primary-nav [aria-current="page"]');
        expect(activeLinks.length).toBe(1);
        expect(activeLinks[0].getAttribute('href')).toBe('/advisor');
        expect(element.querySelector('.logout-button')).toBeNull();
    });

    it('provides an accessible theme control and reflects the dark theme', () => {
        darkMode.set(true);
        fixture.detectChanges();

        const element: HTMLElement = fixture.nativeElement;
        const themeButton = element.querySelector<HTMLButtonElement>('.theme-button');
        expect(themeButton?.getAttribute('aria-label')).toBe('nav.switchToLightMode');
        expect(element.querySelector('.nav-dark')).not.toBeNull();
        themeButton?.click();
        expect(toggleTheme).toHaveBeenCalledTimes(1);
    });

    it('updates authenticated controls when the auth state changes under OnPush', async () => {
        authenticated.set(true);
        await settle();
        const element: HTMLElement = fixture.nativeElement;
        expect(element.querySelectorAll('.primary-nav a').length).toBe(3);
        expect(element.querySelector('.theme-button')).not.toBeNull();
        expect(element.querySelector('.profile-button')).not.toBeNull();
        expect(element.querySelector('.desktop-actions .sign-in-link')).toBeNull();
        expect(element.querySelector('.desktop-actions .sign-up-link')).toBeNull();
        await openMenu('.profile-button');
        expect(menu().textContent).toContain('nav.logout');
        expect(menu().querySelector('.sign-in-link')).toBeNull();

        authenticated.set(false);
        await settle();
        expect(element.querySelector('.primary-nav')).toBeNull();
        expect(element.querySelector('.profile-button')).toBeNull();
        expect(element.querySelector('.theme-button')).not.toBeNull();
        expect(element.querySelector('.desktop-actions .sign-in-link')).not.toBeNull();
        expect(element.querySelector('.desktop-actions .sign-up-link')).not.toBeNull();
        expect(element.querySelector('.logout-button')).toBeNull();
    });

    it('selects a language through the anonymous desktop language menu', async () => {
        await settle();
        expect(button('.desktop-actions .language-button').getAttribute('aria-label'))
            .toBe('nav.language: languages.en');
        const panel = await openMenu('.desktop-actions .language-button');
        const items = panel.querySelectorAll<HTMLButtonElement>('[role="menuitem"]');
        expect(Array.from(items, item => item.querySelector('span')?.textContent?.trim()))
            .toEqual(['languages.en', 'languages.cs', 'languages.de']);
        expect(panel.querySelectorAll('.active-language').length).toBe(1);
        expect(panel.querySelector('.active-language')?.textContent).toContain('languages.en');

        menuButton('languages.cs').click();
        await settle();
        expect(setLanguage).toHaveBeenCalledTimes(1);
        expect(setLanguage).toHaveBeenCalledWith('cs');
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
        expect(button('.desktop-actions .language-button').getAttribute('aria-label'))
            .toBe('nav.language: languages.cs');
    });

    it('selects a language through the authenticated profile submenu', async () => {
        authenticated.set(true);
        await settle();
        await openMenu('.profile-button');
        expect(button('.language-button').getAttribute('aria-label')).toBe('nav.language: languages.en');
        const panel = await openMenu('.language-button');
        const items = panel.querySelectorAll<HTMLButtonElement>('[role="menuitem"]');
        expect(Array.from(items, item => item.querySelector('span')?.textContent?.trim()))
            .toEqual(['languages.en', 'languages.cs', 'languages.de']);

        menuButton('languages.cs').click();
        await settle();
        expect(setLanguage).toHaveBeenCalledTimes(1);
        expect(setLanguage).toHaveBeenCalledWith('cs');
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
        await openMenu('.profile-button');
        expect(button('.language-button').getAttribute('aria-label')).toBe('nav.language: languages.cs');
    });

    it('shows sign in, sign up, language and theme in the anonymous mobile menu', async () => {
        const panel = await openMenu('.mobile-menu-button');
        expect(panel.querySelectorAll('[role="menuitem"]').length).toBe(4);
        expect(panel.querySelector('.sign-in-link')?.tagName).toBe('BUTTON');
        expect(panel.querySelector('.sign-up-link')?.tagName).toBe('BUTTON');
        expect(panel.textContent).toContain('nav.language');
        expect(panel.textContent).toContain('nav.darkMode');
        expect(panel.textContent).not.toContain('nav.logout');
        expect(panel.querySelector('[role="separator"]')).toBeNull();
    });

    it('opens login over the current page from the anonymous desktop sign-in control', async () => {
        const signIn = (fixture.nativeElement as HTMLElement)
            .querySelector<HTMLButtonElement>('.desktop-actions .sign-in-link');
        expect(signIn).not.toBeNull();
        signIn!.click();
        await settle();
        expect(openLogin).toHaveBeenCalledWith();
        expect(TestBed.inject(Router).url).toBe('/');
    });

    it('opens sign-up over the current page from the anonymous desktop sign-up control', async () => {
        const signUp = (fixture.nativeElement as HTMLElement)
            .querySelector<HTMLButtonElement>('.desktop-actions .sign-up-link');
        expect(signUp).not.toBeNull();
        signUp!.click();
        await settle();
        expect(openSignUp).toHaveBeenCalledWith();
        expect(TestBed.inject(Router).url).toBe('/');
    });

    it('shows authenticated mobile navigation and theme without language or logout choices', async () => {
        authenticated.set(true);
        await settle();
        const panel = await openMenu('.mobile-menu-button');
        expect(Array.from(panel.querySelectorAll('a'), link => link.getAttribute('href')))
            .toEqual(['/', '/advisor', '/about', '/contact']);
        expect(panel.textContent).toContain('nav.darkMode');
        expect(panel.textContent).not.toContain('nav.logout');
        expect(panel.textContent).not.toContain('nav.language');
        expect(panel.textContent).not.toContain('languages.');
        expect(panel.querySelectorAll('[role="separator"]').length).toBe(1);
    });

    it('navigates from the mobile menu, closes it and marks home active only on the exact route', async () => {
        authenticated.set(true);
        await TestBed.inject(Router).navigateByUrl('/advisor');
        await settle();
        const panel = await openMenu('.mobile-menu-button');
        expect(panel.querySelectorAll('[aria-current="page"]').length).toBe(1);
        expect(panel.querySelector('[aria-current="page"]')?.getAttribute('href')).toBe('/advisor');
        expect(panel.querySelector('a[href="/"]')?.classList.contains('active-link')).toBe(false);
        const home = panel.querySelector<HTMLAnchorElement>('a[href="/"]');
        expect(home).not.toBeNull();
        home!.click();
        await settle();

        expect(TestBed.inject(Router).url).toBe('/');
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
        const reopened = await openMenu('.mobile-menu-button');
        expect(reopened.querySelectorAll('[aria-current="page"]').length).toBe(1);
        expect(reopened.querySelector('[aria-current="page"]')?.getAttribute('href')).toBe('/');
    });

    it('navigates home through the brand without requiring authentication', async () => {
        const router = TestBed.inject(Router);
        await router.navigateByUrl('/advisor');
        await settle();
        const brand = (fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('.brand-link');
        expect(brand).not.toBeNull();
        expect(brand!.querySelector('.brand-mark')?.getAttribute('aria-hidden')).toBe('true');
        brand!.click();
        await settle();
        expect(router.url).toBe('/');
    });

    it('updates the desktop theme action name, title and icon after toggling', async () => {
        await settle();
        const trigger = button('.theme-button');
        expect(trigger.getAttribute('aria-label')).toBe('nav.switchToDarkMode');
        expect(trigger.getAttribute('title')).toBe('nav.switchToDarkMode');
        trigger.click();
        await settle();
        expect(toggleTheme).toHaveBeenCalledTimes(1);
        expect(trigger.getAttribute('aria-label')).toBe('nav.switchToLightMode');
        expect(trigger.getAttribute('title')).toBe('nav.switchToLightMode');
        expect(trigger.querySelector('mat-icon')?.textContent).toBe('light_mode');
        expect((fixture.nativeElement as HTMLElement).querySelector('.nav-dark')).not.toBeNull();
    });

    it('toggles the theme from mobile and reflects the new action when reopened', async () => {
        await settle();
        await openMenu('.mobile-menu-button');
        menuButton('nav.darkMode').click();
        await settle();
        expect(toggleTheme).toHaveBeenCalledTimes(1);
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
        const reopened = await openMenu('.mobile-menu-button');
        expect(reopened.textContent).toContain('nav.lightMode');
        expect(reopened.textContent).not.toContain('nav.darkMode');
    });

    it('exposes expanded state and closes on Escape with focus restored to the trigger', async () => {
        authenticated.set(true);
        await settle();
        const trigger = button('.profile-button');
        trigger.focus();
        expect(trigger.getAttribute('aria-haspopup')).toBe('menu');
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        const panel = await openMenu('.profile-button');
        expect(trigger.getAttribute('aria-expanded')).toBe('true');
        expect(trigger.getAttribute('aria-controls')).toBe(panel.id);
        expect(panel.contains(document.activeElement)).toBe(true);

        panel.dispatchEvent(new KeyboardEvent('keydown', {
            key: 'Escape', keyCode: 27, bubbles: true,
        }));
        await settle();
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
        expect(trigger.getAttribute('aria-expanded')).toBe('false');
        expect(document.activeElement).toBe(trigger);
    });

    it('closes the menu when its backdrop is clicked', async () => {
        await openMenu('.mobile-menu-button');
        const backdrop = overlayContainer.getContainerElement()
            .querySelector<HTMLElement>('.cdk-overlay-backdrop');
        expect(backdrop).not.toBeNull();
        backdrop!.click();
        await settle();
        expect(button('.mobile-menu-button').getAttribute('aria-expanded')).toBe('false');
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
    });

    it('removes an open overlay when the component is destroyed', async () => {
        authenticated.set(true);
        await settle();
        await openMenu('.profile-button');
        await openMenu('.language-button');
        fixture.destroy();
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
        expect(overlayContainer.getContainerElement().querySelector('.cdk-overlay-backdrop')).toBeNull();
    });

    it('waits for the profile logout response before navigating to login', async () => {
        authenticated.set(true);
        await TestBed.inject(Router).navigateByUrl('/advisor');
        await settle();
        await openMenu('.profile-button');
        expect(menu().querySelectorAll('[role="menuitem"]').length).toBe(2);
        menuButton('nav.logout').click();
        await settle();
        expect(logout).toHaveBeenCalled();
        expect(TestBed.inject(Router).url).toBe('/advisor');

        logoutResponse.next({ success: true, message: 'Logged out' });
        await settle();
        expect(TestBed.inject(Router).url).toBe('/');
        expect(openLogin).toHaveBeenCalledWith();
        expect(overlayContainer.getContainerElement().querySelector('[role="menu"]')).toBeNull();
    });
});
