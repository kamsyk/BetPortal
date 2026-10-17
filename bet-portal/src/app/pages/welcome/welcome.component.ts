import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthDialogService } from '../../services/auth-dialog.service';

@Component({
  selector: 'app-welcome',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe],
  templateUrl: './welcome.component.html',
  styleUrl: './welcome.component.scss',
})
export class WelcomeComponent {
  private readonly authDialog = inject(AuthDialogService);

  signIn(): void {
    this.authDialog.openLogin();
  }
}
