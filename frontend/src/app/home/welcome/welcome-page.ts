import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Card } from 'primeng/card';

import { HomeStore } from '@home/home.store';

@Component({
  imports: [Card, ButtonDirective, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-welcome-page',
  templateUrl: './welcome-page.html',
})
export class WelcomePage {
  protected readonly store = inject(HomeStore);
}
