import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { provideTranslateService } from '@ngx-translate/core';
import { API_BASE_URL } from '../../../core/tokens';
import { UserTokenComponent } from './user-token.component';

describe('UserTokenComponent', () => {
  let token: string | null;
  let http: HttpTestingController;

  beforeEach(() => {
    token = 'synthetic-token';
    TestBed.configureTestingModule({
      imports: [UserTokenComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        provideTranslateService(),
        { provide: API_BASE_URL, useValue: 'http://localhost:5026/api' },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { get queryParamMap() {
            return convertToParamMap(token === null ? {} : { token });
          } } },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('posts the token once in the browser and replaces the URL with Welcome only after success', async () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    const fixture = TestBed.createComponent(UserTokenComponent);
    http.expectNone('http://localhost:5026/api/user-token');
    fixture.detectChanges();
    TestBed.tick();
    const request = http.expectOne('http://localhost:5026/api/user-token');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ token });
    expect(navigate).not.toHaveBeenCalled();
    request.flush(null, { status: 204, statusText: 'No Content' });
    await fixture.whenStable();
    expect(navigate).toHaveBeenCalledExactlyOnceWith('/welcome', { replaceUrl: true });
    fixture.detectChanges();
    TestBed.tick();
    http.expectNone('http://localhost:5026/api/user-token');
  });

  for (const invalid of [null, '', ' ', 'x'.repeat(101)]) {
    it(`rejects a missing or malformed token locally (${invalid?.length ?? 'null'})`, () => {
      token = invalid;
      const fixture = TestBed.createComponent(UserTokenComponent);
      fixture.detectChanges();
      TestBed.tick();
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
        .toContain('userToken.invalid');
      http.expectNone('http://localhost:5026/api/user-token');
    });
  }

  for (const status of [404, 503]) {
    it(`shows activation failure without redirecting (${status})`, async () => {
      const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
      const fixture = TestBed.createComponent(UserTokenComponent);
      fixture.detectChanges();
      TestBed.tick();
      http.expectOne('http://localhost:5026/api/user-token')
        .flush({}, { status, statusText: 'Activation Failed' });
      await fixture.whenStable();
      fixture.detectChanges();
      expect((fixture.nativeElement as HTMLElement).querySelector('[role="alert"]')?.textContent)
        .toContain(status === 404 ? 'userToken.invalid' : 'userToken.failed');
      expect(navigate).not.toHaveBeenCalled();
    });
  }
});
