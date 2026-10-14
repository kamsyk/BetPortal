import { Component, Injectable, input, output } from '@angular/core';

/**
 * Test double for the external ks-ag-controls package.
 * Production builds still resolve the real package; only tsconfig.spec.json maps here.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  logout(): void {
    return undefined;
  }
}

@Component({
  // Same element name as the external ks-ag-controls login component.
  // eslint-disable-next-line @angular-eslint/component-selector
  selector: 'lib-ks-login',
  standalone: true,
  template: '',
})
export class KsLoginComponent {
  readonly loginUrl = input<string>('');
  readonly redirectUrl = input<string>('');
  readonly loginSuccess = output<{ email: string }>();
}
