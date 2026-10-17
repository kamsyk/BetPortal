import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { provideRouter, Router } from '@angular/router';

import { SignUpConfirmationComponent } from './sign-up-confirmation.component';
import { AuthDialogService } from '../../services/auth-dialog.service';

describe('SignUpConfirmationComponent', () => {
  let component: SignUpConfirmationComponent;
  let fixture: ComponentFixture<SignUpConfirmationComponent>;
  const openLogin = vi.fn<AuthDialogService['openLogin']>();

  beforeEach(async () => {
    openLogin.mockClear();
    await TestBed.configureTestingModule({
      imports: [SignUpConfirmationComponent],
      providers: [
        provideTranslateService(),
        provideRouter([]),
        { provide: AuthDialogService, useValue: { openLogin } },
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(SignUpConfirmationComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('shows activation status and a link back to sign in', () => {
    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('h2')?.textContent).toContain('signUpConfirmation.title');
    expect(element.querySelector('p')?.textContent).toContain('signUpConfirmation.message');
    element.querySelector<HTMLButtonElement>('button')?.click();
    expect(openLogin).toHaveBeenCalledWith();
    expect(TestBed.inject(Router).url).toBe('/');
  });
});
