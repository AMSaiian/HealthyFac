import {
  afterNextRender,
  afterRenderEffect,
  Component,
  DestroyRef,
  type ElementRef,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { Tooltip } from 'primeng/tooltip';

/**
 * The text of a grid cell: at most 2 lines with an ellipsis, and the full value in a tooltip only when the text is cut.
 * PrimeNG `showOnEllipsis` measures one line only, so the cut is measured here by the height of the clamped text.
 */
@Component({
  imports: [Tooltip],
  host: { class: 'block max-w-80' },
  selector: 'hf-cell-text',
  templateUrl: './cell-text.html',
})
export class CellText {
  readonly text = input.required<string>();
  /** Strikes only the text, not the content that a caller projects after it. */
  readonly isStruck = input(false);

  private readonly _textElement = viewChild.required<ElementRef<HTMLElement>>('textElement');
  private readonly _isCut = signal(false);
  private readonly _destroyRef = inject(DestroyRef);

  /** Tells whether the text is taller than its 2 line box. */
  readonly isCut = this._isCut.asReadonly();

  constructor() {
    afterRenderEffect(() => {
      this.text();
      this._measure();
    });
    afterNextRender(() => {
      const observer = new ResizeObserver(() => this._measure());
      observer.observe(this._textElement().nativeElement);
      this._destroyRef.onDestroy(() => observer.disconnect());
    });
  }

  private _measure(): void {
    const element = this._textElement().nativeElement;
    this._isCut.set(element.scrollHeight > element.clientHeight);
  }
}
