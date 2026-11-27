import { Component, computed, input, output } from '@angular/core';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Tooltip } from 'primeng/tooltip';

@Component({
  imports: [ButtonDirective, Tooltip, TranslatePipe],
  host: { class: 'inline-flex' },
  selector: 'hf-grid-refresh-button',
  templateUrl: './grid-refresh-button.html',
})
export class GridRefreshButton {
  readonly secondsLeft = input(0);

  readonly refreshRequested = output<void>();

  protected readonly isCoolingDown = computed(() => this.secondsLeft() > 0);

  protected requestRefresh(): void {
    if (this.isCoolingDown()) {
      return;
    }

    this.refreshRequested.emit();
  }
}
