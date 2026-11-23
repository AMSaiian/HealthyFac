import {
  CDK_DRAG_CONFIG,
  CdkDrag,
  type CdkDragEnd,
  type CdkDragMove,
  CdkDragPreview,
  CdkDropList,
} from '@angular/cdk/drag-drop';
import {
  Component,
  computed,
  effect,
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
import { PlainDatePipe } from '@shared/time/plain-date.pipe';
import type { Temporal } from '@shared/time/temporal';

import {
  areDayAreasShown,
  axisOf,
  barLeftCss,
  barTopCss,
  barWidthCss,
  canDragBar,
  canResizeBar,
  columnWidthOf,
  createClickGuard,
  createDwell,
  describeBar,
  DRAG_PREVIEW_Z_INDEX,
  dropOutcomeOf,
  dropTargetOf,
  gapsOf,
  grabOffsetOf,
  HOUR_LABEL_WIDTH_REM,
  hourLabelsOf,
  isPlaceholderShown,
  isPreviewConflict,
  isSkeletonShown,
  LANE_GAP_REM,
  LANE_HEIGHT_REM,
  laneTopPxOf,
  laneUnderOf,
  leftPxOf,
  localRangeOf,
  minutesOfBar,
  NAME_COLUMN_REM,
  nowPercentOf,
  placeBars,
  type PlacedBar,
  type Point,
  type PreviewDrag,
  previewPointOf,
  type PreviewSlot,
  resizeEstimateOf,
  resizeHandleRightPercent,
  resizeShownOf,
  ROW_ATTRIBUTE,
  ROW_PADDING_REM,
  shownHourLabels,
  shownWhileHolding,
  skeletonRowCount,
  snappedStartOf,
  tooltipOf,
  withLiveEnds,
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
const OVERRUN_START_PROPERTY = '--hf-overrun-start';
const DEFAULT_ROOT_FONT_PX = 16;

/**
 * Shows the day of several people as rows on one time axis: the work blocks, the work orders, and "now". An editable
 * board also lets the user drag a bar to another row or time, resize it, and move the day while a drag runs. The board
 * only reports the gestures; the page decides what they change. The bars of an editable board live in one drop list
 * above the rows, so a drag shows a floating preview, and the row under the pointer decides the drop.
 */
@Component({
  imports: [CdkDrag, CdkDragPreview, CdkDropList, EmptyState, Message, PlainDatePipe, Skeleton, Tooltip, TranslatePipe],
  host: { class: 'flex min-h-0 min-w-0 flex-col' },
  providers: [{ provide: CDK_DRAG_CONFIG, useValue: { zIndex: DRAG_PREVIEW_Z_INDEX } }],
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
  /** Whether a drag that started outside the board (the backlog) runs. It shows the day switch areas. */
  readonly isExternalDragActive = input(false);
  readonly pendingIds = input<ReadonlySet<string>>(new Set());
  readonly conflictIds = input<ReadonlySet<string>>(new Set());
  /** The translation key of the reason that the server gave for each failed work order. */
  readonly failures = input<ReadonlyMap<string, string>>(new Map());

  readonly workOrderClick = output<string>();
  readonly barDropped = output<BarDropEvent>();
  readonly barResized = output<BarResizeEvent>();
  readonly barUnassigned = output<string>();
  readonly daySwitchRequested = output<-1 | 1>();
  /** Tells the page that a drag of a bar of the board starts (`true`) or ends (`false`). */
  readonly dragActiveChange = output<boolean>();

  protected readonly isDragging = signal(false);
  protected readonly isDragActive = computed(() => this.isDragging() || this.isExternalDragActive());
  protected readonly showDayAreas = computed(() =>
    areDayAreasShown(this.isEditable(), this.isDragging(), this.isExternalDragActive()),
  );
  protected readonly nameColumnRem = NAME_COLUMN_REM;
  protected readonly rowPaddingRem = ROW_PADDING_REM;
  protected readonly laneHeightRem = LANE_HEIGHT_REM;
  protected readonly laneGapRem = LANE_GAP_REM;
  /** The distance in pixels from the left edge of the grabbed bar to the pointer. The preview keeps it. */
  protected readonly grabOffsetPx = signal(0);
  /** The snapped slot under the preview of the running drag, or `null` outside every row. */
  protected readonly previewSlot = signal<PreviewSlot | null>(null);
  protected readonly isPlaceholderVisible = computed(() =>
    isPlaceholderShown(this._shownDate(), this._dragSourceDate()),
  );

  /** The point that the CDK takes as the pointer of a board bar: on the snapped slot while the pointer is over a row. */
  protected readonly constrain = (point: Point): Point => {
    const bar = this._holdingBar;
    return bar === null ? point : this.previewAt(point, this.grabOffsetPx(), minutesOfBar(bar), bar.id).point;
  };

  /**
   * The rows and the date that the board draws. A drag that runs while the next day loads keeps the previous ones, so
   * the rows stay in the page and the scroll position and the dragged bar stay where they are.
   */
  private readonly _isHolding = computed(() => this.isDragActive() && this.isLoading());
  private readonly _shownRows = linkedSignal<
    { rows: readonly ScheduleRow[]; isHolding: boolean },
    readonly ScheduleRow[]
  >({
    source: () => ({ rows: this.rows(), isHolding: this._isHolding() }),
    computation: (source, previous) => shownWhileHolding(source.rows, previous?.value, source.isHolding),
  });
  private readonly _shownDate = linkedSignal<{ date: Temporal.PlainDate; isHolding: boolean }, Temporal.PlainDate>({
    source: () => ({ date: this.date(), isHolding: this._isHolding() }),
    computation: (source, previous) => shownWhileHolding(source.date, previous?.value, source.isHolding),
  });
  private readonly _delayedLoading = delayedLoading(this.isLoading);
  private readonly _axisWidth = signal(0);
  private readonly _rootFontPx = signal(DEFAULT_ROOT_FONT_PX);

  protected readonly axis = computed(() => axisOf(this._shownDate(), this.timeZoneId()));
  protected readonly hours = computed(() => hourLabelsOf(this.axis(), this.timeZoneId()));
  protected readonly nowPercent = computed(() => nowPercentOf(this.now(), this.axis()));
  protected readonly showSkeleton = computed(() => isSkeletonShown(this._delayedLoading(), this.isDragActive()));
  protected readonly showEmpty = computed(() =>
    isEmptyResult({ rows: this._shownRows(), isLoading: this.isLoading(), hasLoaded: this.hasLoaded() }),
  );
  protected readonly shownLabels = computed(() =>
    shownHourLabels(
      this.hours().length,
      columnWidthOf(this._axisWidth() + this._nameColumnPx(), this._nameColumnPx(), this.hours().length),
      HOUR_LABEL_WIDTH_REM * this._rootFontPx(),
    ),
  );
  /** The width of the time axis in pixels. The page gives it to the backlog for the width of a drag preview. */
  readonly axisWidthPx = this._axisWidth.asReadonly();

  private readonly _lastRowCount = linkedSignal<readonly ScheduleRow[], number>({
    source: this._shownRows,
    computation: (rows, previous) => (rows.length > 0 ? rows.length : (previous?.value ?? 0)),
  });
  protected readonly skeletonRowIndexes = computed(() =>
    Array.from({ length: skeletonRowCount(this._lastRowCount()) }, (_, index) => index),
  );

  private readonly _translate = inject(TranslateService);
  private readonly _overlay = viewChild<ElementRef<HTMLElement>>('overlay');
  private readonly _header = viewChild<ElementRef<HTMLElement>>('header');
  private readonly _draggedBar = signal<FlatBar | null>(null);
  private _holdingBar: ScheduleBar | null = null;
  private readonly _dragSourceDate = signal<Temporal.PlainDate | null>(null);
  private readonly _clickGuard = createClickGuard();
  private readonly _previousDwell = createDwell(() => this.daySwitchRequested.emit(-1));
  private readonly _nextDwell = createDwell(() => this.daySwitchRequested.emit(1));

  protected readonly layouts = computed(() =>
    this._shownRows().map((row) => {
      const { placed, laneCount } = placeBars(withLiveEnds(row.bars, this.now()), this.axis());
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

  constructor() {
    effect((onCleanup) => {
      const header = this._header()?.nativeElement;
      if (header === undefined || typeof ResizeObserver === 'undefined') {
        return;
      }
      const observer = new ResizeObserver(() => this._measure(header));
      observer.observe(header);
      onCleanup(() => observer.disconnect());
    });
  }

  protected barLeft(item: PlacedBar): string {
    return barLeftCss(item.left);
  }

  protected barWidth(item: PlacedBar): string {
    return barWidthCss(item.width);
  }

  protected previewWidthPx(item: PlacedBar): number {
    return (item.width / PERCENT) * this._axisWidth();
  }

  protected tooltip(bar: ScheduleBar): string {
    const failure = this.failures().get(bar.id);
    const text = tooltipOf(bar, this._translate.instant(bar.statusKey), this.timeZoneId(), this._shownDate());
    return failure === undefined ? text : `${text}\n${this._translate.instant(failure)}`;
  }

  protected describe(bar: ScheduleBar): string {
    return describeBar(bar, this.timeZoneId(), this._shownDate());
  }

  protected canDrag(item: PlacedBar): boolean {
    return canDragBar(item);
  }

  protected canResize(item: PlacedBar): boolean {
    return canResizeBar(item, this.axis());
  }

  protected startDate(bar: ScheduleBar): string {
    return bar.start.toZonedDateTimeISO(this.timeZoneId()).toPlainDate().toString();
  }

  protected endDate(bar: ScheduleBar): string {
    return bar.end.toZonedDateTimeISO(this.timeZoneId()).toPlainDate().toString();
  }

  protected handleRight(item: PlacedBar): string {
    return `${resizeHandleRightPercent(item)}%`;
  }

  protected overrunStart(item: PlacedBar): string | null {
    return item.overrunStartPercent === null ? null : `${item.overrunStartPercent}%`;
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
    if (!this._clickGuard.isBlocked()) {
      this.workOrderClick.emit(id);
    }
  }

  protected onBarPointerDown(event: PointerEvent, bar: ScheduleBar): void {
    this._holdingBar = bar;
    const element = event.currentTarget as HTMLElement;
    const rect = element.getBoundingClientRect();
    this.grabOffsetPx.set(grabOffsetOf(event.clientX, rect));
  }

  protected onDragStarted(entry: FlatBar): void {
    this._dragSourceDate.set(this._shownDate());
    this._draggedBar.set(entry);
    this.isDragging.set(true);
    this.dragActiveChange.emit(true);
  }

  protected onDragMoved(event: CdkDragMove): void {
    this.trackExternalDrag(event.pointerPosition);
    const bar = this._draggedBar()?.item.bar;
    this.previewSlot.set(
      bar === undefined
        ? null
        : this.previewAt(event.pointerPosition, this.grabOffsetPx(), minutesOfBar(bar), bar.id).slot,
    );
  }

  protected onDragEnded(event: CdkDragEnd, bar: ScheduleBar): void {
    this._dragSourceDate.set(null);
    this.previewSlot.set(null);
    this._updateDwell(null);
    this.isDragging.set(false);
    this.dragActiveChange.emit(false);
    this._draggedBar.set(null);
    this._clickGuard.markDragged();
    this._dropAt(bar.id, true, event.dropPoint.x - this.grabOffsetPx(), event.dropPoint);
  }

  /** Follows the pointer of a drag that runs outside the board (the backlog): the day switch areas count the dwell. */
  trackExternalDrag(pointer: { readonly x: number; readonly y: number }): void {
    const areas = document.elementsFromPoint(pointer.x, pointer.y);
    const area = areas
      .map((element) => element.closest(`[${DAY_AREA_ATTRIBUTE}]`)?.getAttribute(DAY_AREA_ATTRIBUTE))
      .find((value) => value !== null && value !== undefined);
    this._updateDwell(area ?? null);
  }

  /** Places a bar from outside the board (the backlog): the row under the pointer and the left edge of the preview. */
  placeExternal(workOrderId: string, previewLeftPx: number, pointer: { x: number; y: number }): void {
    this._updateDwell(null);
    this._dropAt(workOrderId, false, previewLeftPx, pointer);
  }

  /**
   * The slot that a preview takes when the pointer is over a row: the start under its left edge snapped to the grid,
   * the end after the estimate, and whether a drop there is refused. The bar of `excludedId` does not count as an
   * obstacle. It is `null` outside every row. The backlog calls it through the page for its own previews.
   */
  previewAt(pointer: Point, grabPx: number, estimateMinutes: number, excludedId: string | null): PreviewDrag {
    const slot = this._slotAt(pointer, grabPx, estimateMinutes, excludedId);
    return { slot, point: previewPointOf(slot, pointer, grabPx, this._rootFontPx()) };
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
    const overlayRect = overlay.getBoundingClientRect();
    const pointerPercent = (clientX: number): number => ((clientX - overlayRect.left) / overlayRect.width) * PERCENT;
    const initialOverrun = bar.style.getPropertyValue(OVERRUN_START_PROPERTY);
    const onMove = (move: PointerEvent): void => {
      const shown = resizeShownOf(item, pointerPercent(move.clientX), this.axis());
      bar.style.width = shown.widthCss;
      if (item.overrunStartPercent !== null) {
        bar.style.setProperty(OVERRUN_START_PROPERTY, `${shown.overrunCssPercent}%`);
      }
      handle.style.right = `${shown.handleRightPercent}%`;
    };
    const onUp = (up: PointerEvent): void => {
      handle.removeEventListener('pointermove', onMove);
      handle.removeEventListener('pointerup', onUp);
      handle.releasePointerCapture(up.pointerId);
      bar.style.width = this.barWidth(item);
      if (item.overrunStartPercent !== null) {
        bar.style.setProperty(OVERRUN_START_PROPERTY, initialOverrun);
      }
      handle.style.right = this.handleRight(item);
      this._clickGuard.markDragged();
      this.barResized.emit({
        workOrderId: item.bar.id,
        estimateMinutes: resizeEstimateOf(item.bar.start, pointerPercent(up.clientX), this.axis()),
      });
    };
    handle.addEventListener('pointermove', onMove);
    handle.addEventListener('pointerup', onUp);
  }

  private _nameColumnPx(): number {
    return NAME_COLUMN_REM * this._rootFontPx();
  }

  private _measure(header: HTMLElement): void {
    const rootFontPx = Number.parseFloat(getComputedStyle(document.documentElement).fontSize);
    const fontPx = Number.isFinite(rootFontPx) && rootFontPx > 0 ? rootFontPx : DEFAULT_ROOT_FONT_PX;
    this._rootFontPx.set(fontPx);
    this._axisWidth.set(Math.max(0, header.getBoundingClientRect().width - NAME_COLUMN_REM * fontPx));
  }

  private _dropAt(
    workOrderId: string,
    isFromBoard: boolean,
    previewLeftPx: number,
    pointer: { readonly x: number; readonly y: number },
  ): void {
    const overlay = this._overlay()?.nativeElement;
    if (overlay === undefined) {
      return;
    }
    const outcome = dropOutcomeOf(
      dropTargetOf(document.elementsFromPoint(pointer.x, pointer.y)),
      isFromBoard,
      previewLeftPx,
      overlay.getBoundingClientRect(),
      this.axis(),
      this.now(),
    );
    if (outcome?.kind === 'unassign') {
      this.barUnassigned.emit(workOrderId);
    } else if (outcome?.kind === 'place') {
      this.barDropped.emit({ workOrderId, assigneeId: outcome.rowId, startInstant: outcome.start });
    }
  }

  private _slotAt(
    pointer: Point,
    grabPx: number,
    estimateMinutes: number,
    excludedId: string | null,
  ): PreviewSlot | null {
    const overlay = this._overlay()?.nativeElement;
    const rowElement = document
      .elementsFromPoint(pointer.x, pointer.y)
      .map((element) => element.closest(`[${ROW_ATTRIBUTE}]`))
      .find((element) => element !== null && element !== undefined);
    const layout = this.layouts().find((entry) => entry.row.id === rowElement?.getAttribute(ROW_ATTRIBUTE));
    if (overlay === undefined || rowElement === null || rowElement === undefined || layout === undefined) {
      return null;
    }
    const axisArea = overlay.getBoundingClientRect();
    const start = snappedStartOf(pointer.x - grabPx, axisArea, this.axis());
    if (start === null) {
      return null;
    }
    const end = start.add({ minutes: estimateMinutes });
    const others = withLiveEnds(layout.row.bars, this.now()).filter((bar) => bar.id !== excludedId);
    const rowTopPx = rowElement.getBoundingClientRect().top;
    const lane = laneUnderOf(pointer.y, rowTopPx, layout.lanes.length, this._rootFontPx());
    return {
      start,
      end,
      rowId: layout.row.id,
      rangeText: localRangeOf(start, end, this.timeZoneId(), this._shownDate()),
      isConflict: isPreviewConflict({ start, end }, layout.row.blocks, others, this.now()),
      leftPx: leftPxOf(start, axisArea, this.axis()),
      laneTopPx: laneTopPxOf(rowTopPx, lane, this._rootFontPx()),
    };
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
