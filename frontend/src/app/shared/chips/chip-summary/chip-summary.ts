import {
  afterNextRender,
  afterRenderEffect,
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  input,
  signal,
  viewChild,
  viewChildren,
} from '@angular/core';
import { Tag } from 'primeng/tag';
import { Tooltip } from 'primeng/tooltip';

import {
  type Chip,
  type ChipAppearance,
  chipGapOf,
  fitChipCount,
  summarizeChips,
} from '@shared/chips/chip-summary/chip-summary.utils';

function isSameList(first: readonly number[], second: readonly number[]): boolean {
  return first.length === second.length && first.every((width, index) => width === second[index]);
}

/**
 * The selected chips of a multi-value select on one row: as many chips as fit, then one `+N` chip for the rest, with
 * every label in a tooltip. A hidden row holds every chip and the `+N` chip, and a resize observer on the host
 * measures them, so the count follows the width and the text of the chips. The host contains its inline size: the chips
 * do not widen a filter menu that sizes to its content, so the count stays under the width of the menu.
 */
@Component({
  imports: [Tag, Tooltip],
  host: {
    class: 'relative flex h-lh w-full min-w-0 flex-nowrap items-center gap-1 overflow-hidden contain-inline-size',
  },
  selector: 'hf-chip-summary',
  templateUrl: './chip-summary.html',
})
export class ChipSummary {
  readonly chips = input.required<readonly Chip[]>();
  readonly placeholder = input('');
  readonly appearance = input<ChipAppearance>('tag');

  private readonly _host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly _destroyRef = inject(DestroyRef);
  private readonly _measuredChips = viewChildren('measuredChip', { read: ElementRef<HTMLElement> });
  private readonly _measuredMore = viewChildren('measuredMore', { read: ElementRef<HTMLElement> });
  private readonly _measureRow = viewChild.required<ElementRef<HTMLElement>>('measureRow');
  private readonly _availableWidth = signal(0);
  private readonly _gap = signal(0);
  private readonly _moreWidth = signal(0);
  private readonly _chipWidths = signal<readonly number[]>([], { equal: isSameList });

  protected readonly summary = computed(() => {
    const chips = this.chips();
    const widths = this._chipWidths();
    const isMeasured = this._availableWidth() > 0 && widths.length === chips.length;
    const shownCount = isMeasured
      ? fitChipCount({
          availableWidth: this._availableWidth(),
          chipWidths: widths,
          gap: this._gap(),
          moreWidth: this._moreWidth(),
        })
      : chips.length;
    return summarizeChips(chips, shownCount);
  });
  protected readonly shownLabels = computed(() =>
    this.summary()
      .shown.map((chip) => chip.label)
      .join(', '),
  );

  constructor() {
    afterRenderEffect({
      read: () => {
        this.chips();
        this.appearance();
        this._measure();
      },
    });
    afterNextRender(() => {
      const observer = new ResizeObserver(() => {
        this._measure();
      });
      observer.observe(this._host.nativeElement);
      observer.observe(this._measureRow().nativeElement);
      this._destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  private _measure(): void {
    const host = this._host.nativeElement;
    this._gap.set(chipGapOf(this.appearance(), parseFloat(getComputedStyle(host).columnGap) || 0));
    this._availableWidth.set(host.clientWidth);
    this._chipWidths.set(this._measuredChips().map((chip) => chip.nativeElement.getBoundingClientRect().width));
    this._moreWidth.set(this._measuredMore()[0]?.nativeElement.getBoundingClientRect().width ?? 0);
  }
}
