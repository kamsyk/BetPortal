import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthDialogService } from '../../services/auth-dialog.service';

@Component({
  selector: 'app-sign-up-confirmation',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe],
  templateUrl: './sign-up-confirmation.component.html',
  styleUrl: './sign-up-confirmation.component.scss',
})
export class SignUpConfirmationComponent {
  private readonly authDialog = inject(AuthDialogService);

  signIn(): void {
    this.authDialog.openLogin();
  }
}
