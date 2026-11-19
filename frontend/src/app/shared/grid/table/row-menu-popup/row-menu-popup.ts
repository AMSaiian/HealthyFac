import { Component, input, viewChild } from '@angular/core';
import type { MenuItem } from 'primeng/api';
import { Menu } from 'primeng/menu';
import { Tooltip } from 'primeng/tooltip';

/** The popup menu of a grid row. Every grid renders its row menu through it, so the items look the same. */
@Component({
  imports: [Menu, Tooltip],
  host: { class: 'contents' },
  selector: 'hf-row-menu-popup',
  templateUrl: './row-menu-popup.html',
})
export class RowMenuPopup {
  readonly model = input.required<MenuItem[]>();

  private readonly _menu = viewChild.required(Menu);

  toggle(event: Event): void {
    this._menu().toggle(event);
  }
}
