import { TestBed } from '@angular/core/testing';

import { TmpAuthService } from './tmp-auth.service';

describe('TmpAuthService', () => {
  let service: TmpAuthService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(TmpAuthService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
