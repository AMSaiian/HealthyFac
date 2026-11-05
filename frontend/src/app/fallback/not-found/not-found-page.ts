import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  imports: [RouterLink, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-1 flex-col' },
  selector: 'hf-not-found-page',
  templateUrl: './not-found-page.html',
})
export class NotFoundPage {}
