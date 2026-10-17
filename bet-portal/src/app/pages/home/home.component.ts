import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';
import { AuthDialogService } from '../../services/auth-dialog.service';

@Component({
  selector: 'app-home',
  imports: [TranslatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
})
export class HomeComponent {
  private readonly authDialog = inject(AuthDialogService);

  signIn(): void {
    this.authDialog.openLogin({ returnUrl: '/advisor' });
  }
}
