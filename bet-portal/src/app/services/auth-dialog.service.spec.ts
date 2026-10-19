import { PLATFORM_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { Subject } from 'rxjs';

import { AuthDialogRequest, AuthDialogService } from './auth-dialog.service';

interface FakeDialogRef {
  readonly close: ReturnType<typeof vi.fn>;
  readonly closed: Subject<void>;
  readonly afterClosed: () => Subject<void>;
}

describe('AuthDialogService', () => {
  let service: AuthDialogService;
  let queryParams: Record<string, string>;
  let dialogOpen: ReturnType<typeof vi.fn>;
  let navigate: ReturnType<typeof vi.fn>;
  let refs: FakeDialogRef[];

  beforeEach(() => {
    queryParams = {};
    refs = [];
    navigate = vi.fn();
    dialogOpen = vi.fn((..._args: unknown[]) => {
      const closed = new Subject<void>();
      const ref: FakeDialogRef = {
        close: vi.fn(),
        closed,
        afterClosed: () => closed,
      };
      refs.push(ref);
      return ref;
    });

    TestBed.configureTestingModule({
      providers: [
        AuthDialogService,
        { provide: PLATFORM_ID, useValue: 'browser' },
        { provide: MatDialog, useValue: { open: dialogOpen } },
        { provide: Router, useValue: { navigate } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { get queryParamMap() { return convertToParamMap(queryParams); } } },
        },
      ],
    });

    service = TestBed.inject(AuthDialogService);
  });

  it('routes account-required requests to sign up and preserves the request data', async () => {
    const request: AuthDialogRequest = {
      returnUrl: '/advisor',
      googleError: 'account-required',
    };

    service.openRequested('login', request);

    await vi.waitFor(() => expect(dialogOpen).toHaveBeenCalledOnce());
    const [component, config] = dialogOpen.mock.calls[0] as [
      { name: string },
      { data: AuthDialogRequest; width: string },
    ];
    expect(component.name).toBe('SignUpComponent');
    expect(config.data).toBe(request);
    expect(config.width).toBe('560px');
  });

  it('opens only the latest dialog when lazy-loaded requests race', async () => {
    service.openLogin({ returnUrl: '/first' });
    service.openSignUp({ returnUrl: '/latest' });

    await vi.waitFor(() => expect(dialogOpen).toHaveBeenCalledOnce());
    const [component, config] = dialogOpen.mock.calls[0] as [
      { name: string },
      { data: AuthDialogRequest },
    ];
    expect(component.name).toBe('SignUpComponent');
    expect(config.data.returnUrl).toBe('/latest');
  });

  it('closes the previous dialog and only lets the active dialog clear auth query parameters', async () => {
    queryParams = { dialog: 'login', returnUrl: '/advisor' };
    service.openLogin();
    await vi.waitFor(() => expect(dialogOpen).toHaveBeenCalledOnce());

    service.openLogin();
    expect(dialogOpen).toHaveBeenCalledOnce();

    service.openSignUp();
    await vi.waitFor(() => expect(dialogOpen).toHaveBeenCalledTimes(2));
    expect(refs[0].close).toHaveBeenCalledOnce();

    refs[0].closed.next();
    expect(navigate).not.toHaveBeenCalled();

    refs[1].closed.next();
    expect(navigate).toHaveBeenCalledWith([], {
      queryParams: {
        dialog: null,
        googleError: null,
        facebookError: null,
        signUp: null,
        returnUrl: null,
      },
      queryParamsHandling: 'merge',
      replaceUrl: true,
    });
  });
});
