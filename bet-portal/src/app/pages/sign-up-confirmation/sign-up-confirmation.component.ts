import { ChangeDetectionStrategy, Component } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-sign-up-confirmation',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TranslatePipe, RouterLink],
  templateUrl: './sign-up-confirmation.component.html',
  styleUrl: './sign-up-confirmation.component.scss',
})
export class SignUpConfirmationComponent {

}
