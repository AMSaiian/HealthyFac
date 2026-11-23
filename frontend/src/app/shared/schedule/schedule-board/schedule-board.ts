import { CdkDrag, type CdkDragEnd, type CdkDragMove, type CdkDragStart } from '@angular/cdk/drag-drop';
import {
  Component,
  computed,
  type ElementRef,
  inject,
  input,
  linkedSignal,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { Message } from 'primeng/message';
import { Skeleton } from 'primeng/skeleton';
import { Tooltip } from 'primeng/tooltip';

import { isEmptyResult } from '@shared/grid/table/grid-display';
import { delayedLoading } from '@shared/loading/delayed-loading';
import { EmptyState } from '@shared/loading/empty-state/empty-state';
import type { ScheduleBar, ScheduleRow } from '@shared/schedule/schedule-row';
import { Temporal } from '@shared/time/temporal';

import {
  axisOf,
  barTopCss,
  createDwell,
  describeBar,
  dropTargetOf,
  gapsOf,
  hourLabelsOf,
  instantAt,
  LANE_GAP_REM,
  LANE_HEIGHT_REM,
  minutesOfWidth,
  NAME_COLUMN_REM,
  nowPercentOf,
  placeBars,
  type PlacedBar,
  ROW_PADDING_REM,
  skeletonRowCount,
  snapToGrid,
  tooltipOf,
} from '@shared/schedule/schedule-board/schedule-board.utils';

export interface BarDropEvent {
  readonly workOrderId: string;
  readonly assigneeId: string;
  readonly startInstant: Temporal.Instant;
}

export interface BarResizeEvent {
  readonly workOrderId: string;
  readonly estimateMinutes: number;
}

interface FlatBar {
  readonly item: PlacedBar;
  readonly rowId: string;
  readonly top: string;
}

const DAY_AREA_ATTRIBUTE = 'data-day-area';
const PREVIOUS_AREA = 'previous';
const NEXT_AREA = 'next';
const PERCENT = 100;

/**
 * Shows the day of several people as rows on one time axis: the work blocks, the work orders, and "now". An editable
 * board also lets the user drag a bar to another row or time, resize it, and move the day while a drag runs. The board
 * only reports the gestures; the page decides what they change.
 */
@Component({
  imports: [CdkDrag, EmptyState, Message, Skeleton, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-col' },
  selector: 'hf-schedule-board',
  styleUrl: './schedule-board.css',
  templateUrl: './schedule-board.html',
})
export class ScheduleBoard {
  readonly rows = input.required<readonly ScheduleRow[]>();
  readonly isLoading = input(false);
  readonly hasLoaded = input(false);
  readonly hasFailed = input(false);
  readonly date = input.required<Temporal.PlainDate>();
  readonly timeZoneId = input.required<string>();
  readonly now = input.required<Temporal.Instant>();
  readonly isEditable = input(false);
  readonly pendingIds = input<ReadonlySet<string>>(new Set());
  readonly conflictIds = input<ReadonlySet<string>>(new Set());
  /** The translation key of the reason that the server gave for each failed work order. */
  readonly failures = input<ReadonlyMap<string, string>>(new Map());

  readonly workOrderClick = output<string>();
  readonly barDropped = output<BarDropEvent>();
  readonly barResized = output<BarResizeEvent>();
  readonly barUnassigned = output<string>();
  readonly daySwitchRequested = output<-1 | 1>();

  protected readonly axis = computed(() => axisOf(this.date(), this.timeZoneId()));
  protected readonly hours = computed(() => hourLabelsOf(this.axis(), this.timeZoneId()));
  protected readonly nowPercent = computed(() => nowPercentOf(this.now(), this.axis()));
  protected readonly showSkeleton = delayedLoading(this.isLoading);
  protected readonly showEmpty = computed(() =>
    isEmptyResult({ rows: this.rows(), isLoading: this.isLoading(), hasLoaded: this.hasLoaded() }),
  );
  protected readonly isDragging = signal(false);
  protected readonly nameColumnRem = NAME_COLUMN_REM;
  protected readonly rowPaddingRem = ROW_PADDING_REM;
  protected readonly laneHeightRem = LANE_HEIGHT_REM;
  protected readonly laneGapRem = LANE_GAP_REM;
  private readonly _lastRowCount = linkedSignal<readonly ScheduleRow[], number>({
    source: this.rows,
    computation: (rows, previous) => (rows.length > 0 ? rows.length : (previous?.value ?? 0)),
  });
  protected readonly skeletonRowIndexes = computed(() =>
    Array.from({ length: skeletonRowCount(this._lastRowCount()) }, (_, index) => index),
  );

  private readonly _translate = inject(TranslateService);
  private readonly _overlay = viewChild<ElementRef<HTMLElement>>('overlay');
  private readonly _draggedBar = signal<FlatBar | null>(null);
  private _justDragged = false;
  private readonly _previousDwell = createDwell(() => this.daySwitchRequested.emit(-1));
  private readonly _nextDwell = createDwell(() => this.daySwitchRequested.emit(1));

  protected readonly layouts = computed(() =>
    this.rows().map((row) => {
      const { placed, laneCount } = placeBars(row.bars, this.axis());
      return {
        row,
        placed,
        gaps: gapsOf(row.blocks, this.axis()),
        lanes: Array.from({ length: laneCount }, (_, lane) => placed.filter((item) => item.lane === lane)),
      };
    }),
  );

  /**
   * Every bar of the editable board in one list, outside the rows. The dragged bar stays in it while the rows change
   * with a day switch, so the drag survives and the drop lands on a row of the new day.
   */
  protected readonly flatBars = computed((): FlatBar[] => {
    const laneCounts = this.layouts().map((layout) => layout.lanes.length);
    const flat = this.layouts().flatMap((layout, rowIndex) =>
      layout.placed.map((item) => ({
        item,
        rowId: layout.row.id,
        top: barTopCss(laneCounts, rowIndex, item.lane),
      })),
    );
    const dragged = this._draggedBar();
    return dragged !== null && !flat.some((entry) => entry.item.bar.id === dragged.item.bar.id)
      ? [...flat, dragged]
      : flat;
  });

  protected tooltip(bar: ScheduleBar): string {
    const failure = this.failures().get(bar.id);
    const text = tooltipOf(bar, this._translate.instant(bar.statusKey), this.timeZoneId());
    return failure === undefined ? text : `${text}\n${this._translate.instant(failure)}`;
  }

  protected describe(bar: ScheduleBar): string {
    return describeBar(bar, this.timeZoneId());
  }

  protected barClasses(bar: ScheduleBar): string {
    const marks = [bar.colorClass];
    if (this.failures().has(bar.id)) {
      marks.push('hf-bar-failed');
    } else if (this.conflictIds().has(bar.id)) {
      marks.push('hf-bar-conflict');
    } else if (this.pendingIds().has(bar.id)) {
      marks.push('hf-bar-pending');
    }
    return marks.join(' ');
  }

  protected onBarClick(id: string): void {
    if (!this._justDragged) {
      this.workOrderClick.emit(id);
    }
  }

  protected onDragStarted(entry: FlatBar, event: CdkDragStart): void {
    void event;
    this._draggedBar.set(entry);
    this.isDragging.set(true);
  }

  protected onDragMoved(event: CdkDragMove): void {
    const areas = document.elementsFromPoint(event.pointerPosition.x, event.pointerPosition.y);
    const area = areas
      .map((element) => element.closest(`[${DAY_AREA_ATTRIBUTE}]`)?.getAttribute(DAY_AREA_ATTRIBUTE))
      .find((value) => value !== null && value !== undefined);
    this._updateDwell(area ?? null);
  }

  protected onDragEnded(event: CdkDragEnd, bar: ScheduleBar): void {
    this._updateDwell(null);
    this.isDragging.set(false);
    this._draggedBar.set(null);
    this._justDragged = true;
    setTimeout(() => {
      this._justDragged = false;
    });
    const rect = event.source.element.nativeElement.getBoundingClientRect();
    const target = dropTargetOf(document.elementsFromPoint(event.dropPoint.x, event.dropPoint.y));
    event.source.reset();
    if (target?.kind === 'staging') {
      this.barUnassigned.emit(bar.id);
    } else if (target?.kind === 'row') {
      this._dropAt(bar.id, target.rowId, rect.left);
    }
  }

  /** Places a bar from outside the board (the staging area): the row under the pointer and the left edge of the bar. */
  placeExternal(workOrderId: string, barLeftPx: number, pointer: { x: number; y: number }): void {
    const target = dropTargetOf(document.elementsFromPoint(pointer.x, pointer.y));
    if (target?.kind === 'row') {
      this._dropAt(workOrderId, target.rowId, barLeftPx);
    }
  }

  protected onResizeStart(event: PointerEvent, item: PlacedBar): void {
    const overlay = this._overlay()?.nativeElement;
    const handle = event.currentTarget as HTMLElement;
    const bar = handle.parentElement;
    if (overlay === undefined || bar === null) {
      return;
    }
    event.preventDefault();
    event.stopPropagation();
    handle.setPointerCapture(event.pointerId);
    const overlayWidth = overlay.getBoundingClientRect().width;
    const startLeft = bar.getBoundingClientRect().left;
    const widthPercent = (clientX: number): number => ((clientX - startLeft) / overlayWidth) * PERCENT;
    const onMove = (move: PointerEvent): void => {
      bar.style.width = `${Math.max(0, widthPercent(move.clientX))}%`;
    };
    const onUp = (up: PointerEvent): void => {
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.releasePointerCapture(up.pointerId);
      bar.style.removeProperty('width');
      this._justDragged = true;
      setTimeout(() => {
        this._justDragged = false;
      });
      this.barResized.emit({
        workOrderId: item.bar.id,
        estimateMinutes: minutesOfWidth(widthPercent(up.clientX), this.axis()),
      });
    };
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
  }

  private _dropAt(workOrderId: string, rowId: string, barLeftPx: number): void {
    const overlay = this._overlay()?.nativeElement;
    if (overlay === undefined) {
      return;
    }
    const rect = overlay.getBoundingClientRect();
    const percent = ((barLeftPx - rect.left) / rect.width) * PERCENT;
    const startInstant = snapToGrid(instantAt(percent, this.axis()));
    if (Temporal.Instant.compare(startInstant, this.now()) < 0) {
      return;
    }
    this.barDropped.emit({ workOrderId, assigneeId: rowId, startInstant });
  }

  private _updateDwell(area: string | null): void {
    if (area === PREVIOUS_AREA) {
      this._previousDwell.enter();
    } else {
      this._previousDwell.leave();
    }
    if (area === NEXT_AREA) {
      this._nextDwell.enter();
    } else {
      this._nextDwell.leave();
    }
  }
}
