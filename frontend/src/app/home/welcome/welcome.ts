import { Component, inject } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { Button } from 'primeng/button';
import { Card } from 'primeng/card';

import { HomeStore } from '@home/home.store';

@Component({
  imports: [Card, Button, TranslatePipe],
  selector: 'hf-welcome',
  templateUrl: './welcome.html',
})
export class Welcome {
  protected readonly store = inject(HomeStore);
}
