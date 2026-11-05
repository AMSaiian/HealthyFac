import { Component, input } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';

@Component({
  imports: [TranslatePipe],
  host: { class: 'flex h-full min-h-0 flex-1 flex-col items-center justify-center gap-3 text-muted-color' },
  selector: 'hf-empty-state',
  templateUrl: './empty-state.html',
})
export class EmptyState {
  readonly icon = input.required<string>();
  readonly messageKey = input.required<string>();
}
