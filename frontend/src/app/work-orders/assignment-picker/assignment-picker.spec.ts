import { type ComponentFixture, TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';

import type { LookupOption } from '@shared/lookups/lookup-options';
import { Temporal } from '@shared/time/temporal';

import type { FreeSpan, ScheduleDay } from '@work-orders/work-order';

import { AssignmentPicker, type DaysRequest, type SpansRequest } from './assignment-picker';
import type { AssignmentValue } from './assignment-picker.utils';

interface SpanRowView {
  span: FreeSpan;
  isTooShort: boolean;
  isChosen: boolean;
}

interface PickerInternals {
  isDayPickerDisabled(): boolean;
  isTimePickerDisabled(): boolean;
  spanRows(): readonly SpanRowView[] | null;
  bounds(): { earliest: Temporal.PlainTime; latest: Temporal.PlainTime } | null;
  state(): AssignmentValue;
  cellClass(cell: { year: number; month: number; day: number }): string;
  onViewChange(event: { month?: number; year?: number }): void;
  onDayChange(value: Temporal.PlainDate | null): void;
  onAssigneeChange(value: string | null): void;
  chooseSpan(row: SpanRowView): void;
}

const TODAY = Temporal.PlainDate.from('2027-06-10');
const EMPLOYEE: LookupOption = { id: 'e1', displayName: 'Ann Lee', isDeleted: false };
const DAYS: ScheduleDay[] = [
  { date: '2027-06-11', hasBlocks: true, longestFreeMinutes: 120 },
  { date: '2027-06-12', hasBlocks: false, longestFreeMinutes: 0 },
];
const LONG_SPAN: FreeSpan = {
  startLocal: '2027-06-11T09:00:00',
  endLocal: '2027-06-11T13:00:00',
  startUtc: '2027-06-11T06:00:00Z',
  endUtc: '2027-06-11T10:00:00Z',
};
const SHORT_SPAN: FreeSpan = {
  startLocal: '2027-06-11T14:00:00',
  endLocal: '2027-06-11T14:30:00',
  startUtc: '2027-06-11T11:00:00Z',
  endUtc: '2027-06-11T11:30:00Z',
};
const EMPTY: AssignmentValue = { assigneeId: null, start: null };
const ASSIGNED: AssignmentValue = { assigneeId: 'e1', start: null };

interface Setup {
  fixture: ComponentFixture<AssignmentPicker>;
  picker: PickerInternals;
  days: DaysRequest[];
  spans: SpansRequest[];
}

function setup(estimate: number | null, initial: AssignmentValue = EMPTY): Setup {
  TestBed.configureTestingModule({ providers: [provideTranslateService()] });
  const fixture = TestBed.createComponent(AssignmentPicker);
  fixture.componentRef.setInput('estimateMinutes', estimate);
  fixture.componentRef.setInput('employees', [EMPLOYEE]);
  fixture.componentRef.setInput('today', TODAY);
  fixture.componentRef.setInput('initialValue', initial);
  fixture.componentRef.setInput('days', DAYS);
  const days: DaysRequest[] = [];
  const spans: SpansRequest[] = [];
  fixture.componentInstance.daysRequested.subscribe((request) => days.push(request));
  fixture.componentInstance.spansRequested.subscribe((request) => spans.push(request));
  fixture.detectChanges();
  return { fixture, picker: fixture.componentInstance as unknown as PickerInternals, days, spans };
}

function openDayWithSpans(estimate: number): Setup {
  const result = setup(estimate, ASSIGNED);
  result.fixture.componentRef.setInput('spans', [LONG_SPAN, SHORT_SPAN]);
  result.picker.onDayChange(Temporal.PlainDate.from('2027-06-11'));
  result.fixture.detectChanges();
  return result;
}

describe('AssignmentPicker', { tags: ['WorkOrders'] }, () => {
  it.each([
    { name: 'WithoutEstimate', estimate: null, assigneeId: 'e1' },
    { name: 'WithoutAssignee', estimate: 60, assigneeId: null },
    { name: 'WithAnInvalidEstimate', estimate: 3, assigneeId: 'e1' },
  ])('dayPicker_$name_IsDisabled', ({ estimate, assigneeId }) => {
    const { picker } = setup(estimate, { assigneeId, start: null });

    expect(picker.isDayPickerDisabled()).toBe(true);
  });

  it('dayPicker_WithEstimateAndAssignee_IsEnabled', () => {
    const { picker } = setup(60, ASSIGNED);

    expect(picker.isDayPickerDisabled()).toBe(false);
  });

  it('monthChange_OnOnePanel_EmitsOneDaysRequestWithTheShownRange', () => {
    const { picker, days, fixture } = setup(60, ASSIGNED);
    days.length = 0;

    picker.onViewChange({ month: 7, year: 2027 });
    fixture.detectChanges();

    expect(days).toHaveLength(1);
    expect(days[0].assigneeId).toBe('e1');
    expect(days[0].from.toString()).toBe('2027-06-28');
    expect(days[0].to.toString()).toBe('2027-08-08');
  });

  it('daysRequest_OnOpenWithAnAssignee_IsSentOnce', () => {
    const { days, fixture } = setup(60, ASSIGNED);
    fixture.componentRef.setInput('estimateMinutes', 90);
    fixture.detectChanges();

    expect(days).toHaveLength(1);
  });

  it('estimateChange_WithTheDaysLoaded_SendsNoRequestAndChangesTheCellClass', () => {
    const { picker, days, fixture } = setup(60, ASSIGNED);
    const cell = { year: 2027, month: 5, day: 11 };
    days.length = 0;
    const before = picker.cellClass(cell);

    fixture.componentRef.setInput('estimateMinutes', 180);
    fixture.detectChanges();

    expect(before).toBe('hf-day-fits');
    expect(picker.cellClass(cell)).toBe('hf-day-tooShort');
    expect(days).toHaveLength(0);
  });

  it('dayClick_WithAnAssignee_EmitsOneSpansRequest', () => {
    const { picker, spans, fixture } = setup(60, ASSIGNED);

    picker.onDayChange(Temporal.PlainDate.from('2027-06-11'));
    fixture.detectChanges();

    expect(spans).toEqual([{ assigneeId: 'e1', date: Temporal.PlainDate.from('2027-06-11') }]);
  });

  it('spanRows_WithAShortSpan_MarkItTooShort', () => {
    const { picker } = openDayWithSpans(60);

    expect(picker.spanRows()?.map((row) => row.isTooShort)).toEqual([false, true]);
  });

  it('chooseSpan_OnATooShortSpan_KeepsTheStart', () => {
    const { picker } = openDayWithSpans(60);
    const rows = picker.spanRows() ?? [];

    picker.chooseSpan(rows[1]);

    expect(picker.state().start).toBeNull();
  });

  it('chooseSpan_OnAFittingSpan_SetsTheStartAndTheTimeBounds', () => {
    const { picker, fixture } = openDayWithSpans(60);
    const rows = picker.spanRows() ?? [];

    picker.chooseSpan(rows[0]);
    fixture.detectChanges();

    expect(picker.state().start?.toString()).toBe('2027-06-11T09:00:00');
    expect(picker.bounds()?.earliest.toString()).toBe('09:00:00');
    expect(picker.bounds()?.latest.toString()).toBe('12:00:00');
  });

  it('estimateChange_ThatNoLongerFitsTheStart_ClearsTheStart', () => {
    const { picker, fixture } = setup(60, {
      assigneeId: 'e1',
      start: Temporal.PlainDateTime.from('2027-06-11T12:00:00'),
    });
    fixture.componentRef.setInput('spans', [LONG_SPAN]);
    fixture.detectChanges();
    const before = picker.state().start;

    fixture.componentRef.setInput('estimateMinutes', 120);
    fixture.detectChanges();

    expect(before?.toString()).toBe('2027-06-11T12:00:00');
    expect(picker.state().start).toBeNull();
  });

  it('storedStart_BeforeTheFirstFreeSpan_StaysAndStaysValid', () => {
    const { picker, fixture } = setup(60, {
      assigneeId: 'e1',
      start: Temporal.PlainDateTime.from('2027-06-11T08:00:00'),
    });
    const validity: boolean[] = [];
    fixture.componentInstance.validityChange.subscribe((isValid) => validity.push(isValid));

    fixture.componentRef.setInput('spans', [LONG_SPAN]);
    fixture.detectChanges();

    expect(picker.state().start?.toString()).toBe('2027-06-11T08:00:00');
    expect(validity).not.toContain(false);
  });

  it('estimateChange_OnAStoredStartBeforeTheFirstSpan_KeepsTheStartAndStaysValid', () => {
    const { picker, fixture } = setup(60, {
      assigneeId: 'e1',
      start: Temporal.PlainDateTime.from('2027-06-11T08:00:00'),
    });
    const validity: boolean[] = [];
    fixture.componentInstance.validityChange.subscribe((isValid) => validity.push(isValid));
    fixture.componentRef.setInput('spans', [LONG_SPAN]);
    fixture.detectChanges();

    fixture.componentRef.setInput('estimateMinutes', 120);
    fixture.detectChanges();

    expect(picker.state().start?.toString()).toBe('2027-06-11T08:00:00');
    expect(validity).not.toContain(false);
  });

  it('assigneeChange_ToAnotherPerson_ClearsTheDayAndTheStart', () => {
    const { picker, fixture } = setup(60, {
      assigneeId: 'e1',
      start: Temporal.PlainDateTime.from('2027-06-11T09:00:00'),
    });

    picker.onAssigneeChange('e2');
    fixture.detectChanges();

    expect(picker.state()).toEqual({ assigneeId: 'e2', start: null });
    expect(picker.isTimePickerDisabled()).toBe(true);
  });

  it('validity_AfterAnAssigneeChange_IsInvalidUntilAStartIsSet', () => {
    const { picker, fixture } = setup(60, EMPTY);
    const validity: boolean[] = [];
    fixture.componentInstance.validityChange.subscribe((isValid) => validity.push(isValid));

    picker.onAssigneeChange('e1');
    fixture.detectChanges();

    expect(validity.at(-1)).toBe(false);
  });
});
