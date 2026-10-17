import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';

import { WelcomeComponent } from './welcome.component';
import { AuthDialogService } from '../../services/auth-dialog.service';

describe('WelcomeComponent', () => {
  let component: WelcomeComponent;
  let fixture: ComponentFixture<WelcomeComponent>;
  const openLogin = vi.fn<AuthDialogService['openLogin']>();

  beforeEach(async () => {
    openLogin.mockClear();
    await TestBed.configureTestingModule({
      imports: [WelcomeComponent],
      providers: [
        provideRouter([]),
        provideTranslateService(),
        { provide: AuthDialogService, useValue: { openLogin } },
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(WelcomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows the activation message and opens Sign in over the current page', () => {
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('h1')?.textContent).toContain('welcome.title');
    expect(element.querySelector('p')?.textContent).toContain('welcome.message');
    element.querySelector<HTMLButtonElement>('button')?.click();
    expect(openLogin).toHaveBeenCalledWith();
    expect(TestBed.inject(Router).url).toBe('/');
  });
});
