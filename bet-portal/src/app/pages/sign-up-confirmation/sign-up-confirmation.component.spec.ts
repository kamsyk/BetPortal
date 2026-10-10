import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { provideRouter } from '@angular/router';

import { SignUpConfirmationComponent } from './sign-up-confirmation.component';

describe('SignUpConfirmationComponent', () => {
  let component: SignUpConfirmationComponent;
  let fixture: ComponentFixture<SignUpConfirmationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SignUpConfirmationComponent],
      providers: [provideTranslateService(), provideRouter([])],
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
    expect(element.querySelector('h1')?.textContent).toContain('signUpConfirmation.title');
    expect(element.querySelector('p')?.textContent).toContain('signUpConfirmation.message');
    expect(element.querySelector('a')?.getAttribute('href')).toBe('/login');
  });
});
