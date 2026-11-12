import { Component, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslatePipe } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Checkbox } from 'primeng/checkbox';
import { Popover } from 'primeng/popover';
import { Tooltip } from 'primeng/tooltip';

@Component({
  imports: [ButtonDirective, Checkbox, FormsModule, Popover, Tooltip, TranslatePipe],
  host: { class: 'inline-flex' },
  selector: 'hf-column-chooser',
  templateUrl: './column-chooser.html',
})
export class ColumnChooser {
  readonly columns = input.required<readonly string[]>();
  readonly visible = input.required<readonly string[]>();
  readonly labelKeys = input.required<Readonly<Record<string, string>>>();

  readonly toggled = output<{ column: string; isVisible: boolean }>();

  protected isVisible(column: string): boolean {
    return this.visible().includes(column);
  }

  protected onToggle(column: string, isVisible: boolean): void {
    this.toggled.emit({ column, isVisible });
  }
}
