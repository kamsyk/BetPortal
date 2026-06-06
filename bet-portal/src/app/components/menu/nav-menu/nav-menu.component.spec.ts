import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  provideTranslateService,
  TranslateLoader,
  provideTranslateLoader,
} from '@ngx-translate/core';
import { Observable, of } from 'rxjs';

import { NavMenuComponent } from './nav-menu.component';

class FakeTranslateLoader implements TranslateLoader {
  getTranslation(): Observable<Record<string, string>> {
    return of({});
  }
}

describe('NavMenuComponent', () => {
  let component: NavMenuComponent;
  let fixture: ComponentFixture<NavMenuComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NavMenuComponent],
      providers: [
        provideTranslateService({
          loader: provideTranslateLoader(FakeTranslateLoader),
        }),
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(NavMenuComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
