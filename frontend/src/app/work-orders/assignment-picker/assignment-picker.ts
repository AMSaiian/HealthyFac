import { Component, computed, effect, inject, input, linkedSignal, output, signal } from '@angular/core';
import { form, FormField } from '@angular/forms/signals';
import { TranslatePipe, TranslateService } from '@ngx-translate/core';
import { ButtonDirective } from 'primeng/button';
import { Divider } from 'primeng/divider';
import { InputText } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { Tooltip } from 'primeng/tooltip';

import { delayedLoading } from '@shared/loading/delayed-loading';
import { LoadingSpinner } from '@shared/loading/loading-spinner/loading-spinner';
import { keepSelectedOptions, type LookupOption, type LookupRequest } from '@shared/lookups/lookup-options';
import { PlainDatePicker, type ShownMonth } from '@shared/time/pickers/plain-date-picker/plain-date-picker';
import { PlainTimePicker } from '@shared/time/pickers/plain-time-picker/plain-time-picker';
import { Temporal } from '@shared/time/temporal';

import {
  type AssignmentValue,
  cellKeyOf,
  dayClassOf,
  DayLabel,
  dayLabelOf,
  isBeforeFirstSpan,
  isStartAllowed,
  type PickerCell,
  shownRange,
  spanMinutes,
  SpanState,
  spanStateOf,
  startBounds,
} from '@work-orders/assignment-picker/assignment-picker.utils';
import type { FreeSpan, ScheduleDay } from '@work-orders/work-order';
import { ESTIMATE_MAX_MINUTES, ESTIMATE_MIN_MINUTES, GRID_STEP_MINUTES } from '@work-orders/work-order-limits';

const FIRST_DAY_OF_WEEK_KEY = 'PRIMENG.firstDayOfWeek';
const FIRST_DAY_OF_WEEK_FALLBACK = 1;
const LABEL_KEY_PREFIX = 'WORK_ORDERS.PICKER.LABELS.';

/** The dates that the picker asks the day summary for. */
export interface DaysRequest {
  readonly assigneeId: string;
  readonly from: Temporal.PlainDate;
  readonly to: Temporal.PlainDate;
}

/** The day that the picker asks the free spans for. */
export interface SpansRequest {
  readonly assigneeId: string;
  readonly date: Temporal.PlainDate;
}

interface DayCell {
  readonly cssClass: string;
  readonly ariaLabel: string;
}

interface SpanRow {
  readonly span: FreeSpan;
  readonly startText: string;
  readonly endText: string;
  readonly minutes: number;
  readonly isTooShort: boolean;
  readonly isChosen: boolean;
}

function toLabelKey(label: DayLabel): string {
  const parts: Record<DayLabel, string> = {
    [DayLabel.Fits]: 'FITS',
    [DayLabel.TooShort]: 'TOO_SHORT',
    [DayLabel.Busy]: 'BUSY',
    [DayLabel.DayOff]: 'DAY_OFF',
  };
  return `${LABEL_KEY_PREFIX}${parts[label]}`;
}

/**
 * The assignee, the day, the free spans, and the start of a work order. A dumb component: the host loads the day
 * summary and the free spans after `daysRequested` and `spansRequested`. A month change sends one days request; an
 * estimate change sends none and recomputes the labels.
 */
@Component({
  imports: [
    ButtonDirective,
    Divider,
    FormField,
    InputText,
    LoadingSpinner,
    PlainDatePicker,
    PlainTimePicker,
    Select,
    Tooltip,
    TranslatePipe,
  ],
  host: { class: 'flex flex-col gap-4' },
  selector: 'hf-assignment-picker',
  templateUrl: './assignment-picker.html',
})
export class AssignmentPicker {
  private readonly _translate = inject(TranslateService);

  readonly estimateMinutes = input.required<number | null>();
  readonly employees = input.required<LookupOption[]>();
  readonly isEmployeesLoading = input(false);
  readonly days = input<readonly ScheduleDay[] | null>(null);
  readonly isDaysLoading = input(false);
  readonly spans = input<readonly FreeSpan[] | null>(null);
  readonly isSpansLoading = input(false);
  readonly today = input.required<Temporal.PlainDate>();
  readonly initialValue = input.required<AssignmentValue>();
  readonly isReadOnly = input(false);

  readonly valueChange = output<AssignmentValue>();
  readonly validityChange = output<boolean>();
  readonly employeeLookupRequested = output<LookupRequest>();
  readonly daysRequested = output<DaysRequest>();
  readonly spansRequested = output<SpansRequest>();

  protected readonly stepMinute = GRID_STEP_MINUTES;
  protected readonly showEmployeesLoading = delayedLoading(this.isEmployeesLoading);
  protected readonly showDaysLoading = delayedLoading(this.isDaysLoading);
  protected readonly showSpansLoading = delayedLoading(this.isSpansLoading);

  protected readonly state = linkedSignal<AssignmentValue>(() => ({ ...this.initialValue() }));
  protected readonly day = linkedSignal<Temporal.PlainDate | null>(
    () => this.initialValue().start?.toPlainDate() ?? null,
  );
  protected readonly chosenSpanStart = signal<string | null>(null);
  protected readonly viewMonth = linkedSignal<Temporal.PlainYearMonth>(() =>
    (this.initialValue().start?.toPlainDate() ?? this.today()).toPlainYearMonth(),
  );

  protected readonly selection = linkedSignal(() => ({ assigneeId: this.state().assigneeId }));
  protected readonly selectionForm = form(this.selection);
  protected readonly employeeOptions = keepSelectedOptions(this.employees, () => [this.state().assigneeId]);
  protected readonly isEstimateValid = computed(() => {
    const estimate = this.estimateMinutes();
    return estimate !== null && estimate >= ESTIMATE_MIN_MINUTES && estimate <= ESTIMATE_MAX_MINUTES;
  });
  protected readonly isDayPickerDisabled = computed(
    () => this.isReadOnly() || !this.isEstimateValid() || this.state().assigneeId === null,
  );
  protected readonly startText = computed(
    () => this.state().start?.toString({ smallestUnit: 'minute' }).replace('T', ' ') ?? '',
  );

  protected readonly cells = computed(() => {
    const days = this.days();
    const estimate = this.estimateMinutes();
    const result = new Map<string, DayCell>();
    if (days === null || estimate === null) {
      return result;
    }
    for (const day of days) {
      const label = dayLabelOf(day, estimate);
      result.set(day.date, {
        cssClass: dayClassOf(label),
        ariaLabel: `${day.date}: ${this._translate.instant(toLabelKey(label))}`,
      });
    }
    return result;
  });

  protected readonly daySpans = computed(() => {
    const spans = this.spans();
    const day = this.day();
    if (spans === null || day === null) {
      return null;
    }
    return spans.filter((span) => Temporal.PlainDateTime.from(span.startLocal).toPlainDate().equals(day));
  });

  protected readonly activeSpanStart = computed(() => {
    const chosen = this.chosenSpanStart();
    if (chosen !== null) {
      return chosen;
    }
    const start = this.state().start;
    const estimate = this.estimateMinutes();
    if (start === null || estimate === null) {
      return null;
    }
    const containing = this.daySpans()?.find((span) => isStartAllowed(start, [span], estimate));
    return containing?.startLocal ?? null;
  });

  protected readonly spanRows = computed((): readonly SpanRow[] | null => {
    const spans = this.daySpans();
    const estimate = this.estimateMinutes();
    if (spans === null || estimate === null) {
      return null;
    }
    const chosen = this.activeSpanStart();
    return spans.map((span) => ({
      span,
      startText: Temporal.PlainDateTime.from(span.startLocal).toPlainTime().toString({ smallestUnit: 'minute' }),
      endText: Temporal.PlainDateTime.from(span.endLocal).toPlainTime().toString({ smallestUnit: 'minute' }),
      minutes: spanMinutes(span),
      isTooShort: spanStateOf(span, estimate) === SpanState.TooShort,
      isChosen: span.startLocal === chosen,
    }));
  });

  protected readonly bounds = computed(() => {
    const estimate = this.estimateMinutes();
    const chosen = this.activeSpanStart();
    const span = this.daySpans()?.find((candidate) => candidate.startLocal === chosen);
    return estimate === null || span === undefined ? null : startBounds(span, estimate);
  });
  protected readonly timeValue = computed(() => this.state().start?.toPlainTime() ?? null);
  protected readonly isTimePickerDisabled = computed(() => this.isReadOnly() || this.bounds() === null);

  private readonly _hasEstimateChanged = linkedSignal(() => {
    this.initialValue();
    return false;
  });
  private _lastEstimate: number | null | undefined = undefined;
  private _lastDaysKey: string | null = null;
  private _lastSpansKey: string | null = null;

  constructor() {
    effect(() => {
      const estimate = this.estimateMinutes();
      if (this._lastEstimate !== undefined && estimate !== this._lastEstimate) {
        this._hasEstimateChanged.set(true);
      }
      this._lastEstimate = estimate;
    });
    effect(() => {
      const selected = this.selection().assigneeId;
      if (selected !== this.state().assigneeId) {
        this.onAssigneeChange(selected);
      }
    });
    effect(() => {
      this.valueChange.emit(this.state());
    });
    effect(() => {
      const { assigneeId, start } = this.state();
      this.validityChange.emit((assigneeId === null) === (start === null));
    });
    effect(() => {
      const assigneeId = this.state().assigneeId;
      if (this.isDayPickerDisabled() || assigneeId === null) {
        return;
      }
      this._requestDays(assigneeId);
    });
    effect(() => {
      const assigneeId = this.state().assigneeId;
      const day = this.day();
      if (this.isDayPickerDisabled() || assigneeId === null || day === null) {
        return;
      }
      const key = `${assigneeId}|${day.toString()}`;
      if (key !== this._lastSpansKey) {
        this._lastSpansKey = key;
        this.spansRequested.emit({ assigneeId, date: day });
      }
    });
    effect(() => {
      const start = this.state().start;
      const spans = this.daySpans();
      const estimate = this.estimateMinutes();
      if (this.isReadOnly() || start === null || spans === null || estimate === null || this.isSpansLoading()) {
        return;
      }
      const stored = this.initialValue().start;
      if (stored !== null && start.equals(stored) && (!this._hasEstimateChanged() || isBeforeFirstSpan(start, spans))) {
        return;
      }
      if (!isStartAllowed(start, spans, estimate)) {
        this.state.update((value) => ({ ...value, start: null }));
      }
    });
  }

  protected onAssigneeChange(assigneeId: string | null): void {
    if (assigneeId === this.state().assigneeId) {
      return;
    }
    this.state.set({ assigneeId, start: null });
    this.day.set(null);
    this.chosenSpanStart.set(null);
  }

  protected onDayChange(value: Temporal.PlainDate | null): void {
    this.day.set(value);
    this.state.update((current) => ({ ...current, start: null }));
    this.chosenSpanStart.set(null);
  }

  protected onViewChange(event: ShownMonth): void {
    this.viewMonth.set(new Temporal.PlainYearMonth(event.year, event.month));
    const assigneeId = this.state().assigneeId;
    if (!this.isDayPickerDisabled() && assigneeId !== null) {
      this._requestDays(assigneeId);
    }
  }

  protected onPanelShow(): void {
    const assigneeId = this.state().assigneeId;
    if (!this.isDayPickerDisabled() && assigneeId !== null) {
      this._requestDays(assigneeId);
    }
  }

  protected chooseSpan(row: SpanRow): void {
    if (row.isTooShort || this.isReadOnly()) {
      return;
    }
    this.chosenSpanStart.set(row.span.startLocal);
    this.state.update((current) => ({ ...current, start: Temporal.PlainDateTime.from(row.span.startLocal) }));
  }

  protected onTimeChange(time: Temporal.PlainTime | null): void {
    const day = this.day();
    this.state.update((current) => ({
      ...current,
      start: day === null || time === null ? null : day.toPlainDateTime(time),
    }));
  }

  protected cellClass(cell: PickerCell): string {
    return this.cells().get(cellKeyOf(cell))?.cssClass ?? '';
  }

  protected cellAria(cell: PickerCell): string | null {
    return this.cells().get(cellKeyOf(cell))?.ariaLabel ?? null;
  }

  private _requestDays(assigneeId: string): void {
    const month = this.viewMonth();
    const key = `${assigneeId}|${month.toString()}`;
    if (key === this._lastDaysKey) {
      return;
    }
    this._lastDaysKey = key;
    const parsedFirstDay = Number(this._translate.instant(FIRST_DAY_OF_WEEK_KEY));
    const firstDayOfWeek = Number.isNaN(parsedFirstDay) ? FIRST_DAY_OF_WEEK_FALLBACK : parsedFirstDay;
    const range = shownRange(month, firstDayOfWeek);
    this.daysRequested.emit({ assigneeId, from: range.from, to: range.to });
  }
}
