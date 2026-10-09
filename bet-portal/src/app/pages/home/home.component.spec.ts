import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideTranslateService, TranslateService } from '@ngx-translate/core';

import { HomeComponent } from './home.component';

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [provideRouter([]), provideTranslateService()],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('introduces the portal with a single page title and three feature cards', () => {
    expect(component).toBeTruthy();
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelectorAll('h1').length).toBe(1);
    expect(element.querySelectorAll('.feature-card').length).toBe(3);
    expect(element.querySelectorAll('.steps li').length).toBe(3);
    expect(element.querySelector('footer')?.textContent).toContain('home.disclaimer');
  });

  it('offers advisor access and sign-in with the advisor return URL', () => {
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.primary')?.getAttribute('href')).toBe('/advisor');
    expect(element.querySelector('.sign-in')?.getAttribute('href'))
      .toBe('/login?returnUrl=%2Fadvisor');
    expect(element.querySelector('.secondary')?.getAttribute('href')).toBe('#portal-features');
  });

  it('updates the page title when the language changes under OnPush', async () => {
    const translate = TestBed.inject(TranslateService);
    translate.setTranslation('en', { home: { title: 'Portal data', titleAccent: 'A clearer view' } });
    translate.setTranslation('de', { home: { title: 'Portaldaten', titleAccent: 'Ein klarer Blick' } });
    for (const [language, title] of [['en', 'Portal data'], ['de', 'Portaldaten']]) {
      translate.use(language);
      fixture.detectChanges();
      await fixture.whenStable();
      const element: HTMLElement = fixture.nativeElement;
      expect(element.querySelector('h1')?.textContent).toContain(title);
    }
  });
});
