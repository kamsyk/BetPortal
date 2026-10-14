import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BreakpointObserver } from '@angular/cdk/layout';
import { MatDialog } from '@angular/material/dialog';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { NavMenuComponent } from './nav-menu.component';
import { AuthService } from '../../../services/auth.service';

describe('NavMenuComponent', () => {
  let component: NavMenuComponent;
  let fixture: ComponentFixture<NavMenuComponent>;
  let logout: jasmine.Spy;
  let navigateByUrl: jasmine.Spy;

  beforeEach(async () => {
    logout = jasmine.createSpy('logout').and.returnValue(of(undefined));
    navigateByUrl = jasmine.createSpy('navigateByUrl').and.returnValue(Promise.resolve(true));

    await TestBed.configureTestingModule({
      imports: [NavMenuComponent],
      providers: [
        { provide: AuthService, useValue: { logout } },
        { provide: Router, useValue: { navigateByUrl } },
        { provide: BreakpointObserver, useValue: { observe: () => of({ matches: false }) } },
        { provide: MatDialog, useValue: {} },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NavMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('ends the server session before opening the login page', () => {
    component.logOut();

    expect(logout).toHaveBeenCalled();
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
