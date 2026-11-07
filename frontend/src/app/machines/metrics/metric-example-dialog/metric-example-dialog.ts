import { Component, input, output, signal } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Dialog } from 'primeng/dialog';

import { EmptyState } from '@shared/loading/empty-state/empty-state';

@Component({
  imports: [ButtonDirective, Dialog, EmptyState, TranslatePipe],
  host: { class: 'contents' },
  selector: 'hf-metric-example-dialog',
  templateUrl: './metric-example-dialog.html',
})
export class MetricExampleDialog {
  readonly revision = input.required<number>();
  readonly text = input.required<string | null>();
  readonly closed = output<void>();

  protected readonly isVisible = signal(true);
  protected readonly contentStyle = { overflow: 'hidden' };

  protected copy(): void {
    const text = this.text();
    if (text !== null) {
      void navigator.clipboard.writeText(text);
    }
  }
}
