import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { PlatformNavigation } from '@angular/common';
import { ApplicationRef, Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';
import { z } from 'zod/mini';

import { provideActionHosts } from '@shared/actions/action-registry';
import { ActionStore } from '@shared/actions/action.store';
import type { FakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { provideFakePlatformNavigation } from '@shared/history/fake-platform-navigation';
import { AuthStore } from '@shared/security/auth-store/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { EmployeesApi } from '@employees/employees-api';
import { type ShiftOverride, type ShiftVersion, Weekday } from '@employees/shifts/shift';
import {
  affectedOf,
  emptyDays,
  isOverrideDateTaken,
  monthRange,
  normalizeVersion,
  toShiftDialog,
  toVersionFormValue,
} from '@employees/shifts/shifts-store/shifts.store.utils';
import { versionFormSchema } from '@employees/shifts/version-dialog/version-dialog-schema';
import type { VersionFormValue } from '@employees/shifts/version-dialog/version-dialog-schema';

import { DayScheduleSource } from '@work-orders/work-order';
import { WORK_ORDER_COMPOSE, WorkOrderActionType } from '@work-orders/work-order-actions';
import { WorkOrdersApi } from '@work-orders/work-orders-api';

import { ShiftsStore } from './shifts.store';

@Component({ template: '', host: { class: 'contents' } })
class Blank {}

const VERSION: ShiftVersion = normalizeVersion({
  id: 'v1',
  effectiveFrom: '2027-07-01',
  rowVersion: 4,
  days: [{ weekday: Weekday.Monday, blocks: [{ start: '08:00:00', end: '12:00:00' }] }],
});

const OVERRIDE: ShiftOverride = { id: 'o1', date: '2027-06-10', blocks: [], rowVersion: 2 };

const FORM: VersionFormValue = {
  effectiveFrom: Temporal.PlainDate.from('2027-07-05'),
  days: emptyDays().map((day) =>
    day.weekday === Weekday.Monday ? { ...day, blocks: [{ start: '09:00', end: '13:00' }] } : day,
  ),
};

function affectedError(): HttpErrorResponse {
  return new HttpErrorResponse({
    status: HttpStatusCode.UnprocessableEntity,
    error: { workOrders: [{ id: 'wo-1', title: 'Replace filter', plannedStartLocal: '2027-06-10T09:00:00' }] },
  });
}

describe('ShiftsStore', () => {
  const shiftVersions = vi.fn();
  const shiftOverrides = vi.fn();
  const createVersion = vi.fn();
  const updateVersion = vi.fn();
  const deleteVersion = vi.fn();
  const setOverride = vi.fn();
  const deleteOverride = vi.fn();
  const daySchedule = vi.fn();
  const add = vi.fn();
  const confirm = vi.fn();
  let currentNow = Temporal.Instant.from('2027-06-01T09:00:00Z');

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(
    options: { isConfirmed?: boolean; isActionStoreReal?: boolean } = {},
  ): Promise<InstanceType<typeof ShiftsStore>> {
    TestBed.configureTestingModule({
      providers: [
        ShiftsStore,
        {
          provide: EmployeesApi,
          useValue: {
            shiftVersions,
            shiftOverrides,
            createVersion,
            updateVersion,
            deleteVersion,
            setOverride,
            deleteOverride,
          },
        },
        { provide: WorkOrdersApi, useValue: { daySchedule } },
        { provide: AuthStore, useValue: { hasPermission: () => true } },
        provideRouter([{ path: '**', component: Blank }]),
        provideFakePlatformNavigation(),
        ...(options.isActionStoreReal === true
          ? [
              provideActionHosts([
                {
                  type: WorkOrderActionType.Open,
                  compose: WORK_ORDER_COMPOSE,
                  load: () =>
                    Promise.resolve({
                      component: class WorkOrderHost {},
                      schema: z.object({ workOrderId: z.optional(z.string()) }),
                    }),
                },
              ]),
            ]
          : [{ provide: ActionStore, useValue: { dispatch: vi.fn() } }]),
        { provide: MessageService, useValue: { add } },
        {
          provide: ConfirmationService,
          useValue: {
            confirm: confirm.mockImplementation((confirmation: { accept?: () => void }) =>
              options.isConfirmed === true ? confirmation.accept?.() : undefined,
            ),
          },
        },
        { provide: TranslateService, useValue: { instant: (key: string) => key } },
        {
          provide: CLOCK,
          useValue: { now: () => currentNow, timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(ShiftsStore);
    if (options.isActionStoreReal === true) {
      TestBed.inject(ActionStore);
    }
    await TestBed.inject(Router).navigateByUrl('/shifts');
    store.setEmployee('e1');
    await settle();
    return store;
  }

  beforeEach(() => {
    shiftVersions.mockReset().mockReturnValue(of([VERSION]));
    shiftOverrides.mockReset().mockReturnValue(of([OVERRIDE]));
    for (const mock of [createVersion, updateVersion, deleteVersion, setOverride, deleteOverride]) {
      mock.mockReset().mockReturnValue(of(undefined));
    }
    daySchedule.mockReset().mockReturnValue(
      of({
        date: '2027-06-02',
        timeZoneId: 'Europe/Kyiv',
        source: DayScheduleSource.Template,
        blocks: [{ startLocal: '08:00:00', endLocal: '17:00:00', startUtc: '', endUtc: '' }],
        workOrders: [],
      }),
    );
    add.mockReset();
    confirm.mockClear();
    currentNow = Temporal.Instant.from('2027-06-01T09:00:00Z');
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('setEmployee_OnFirstLoad_ReadsTheVersionsAndTheOverridesOfTheCurrentMonth', async () => {
    await setup();

    expect(shiftVersions).toHaveBeenCalledWith('e1');
    expect(shiftOverrides).toHaveBeenCalledWith('e1', '2027-06-01', '2027-06-30');
  });

  it('saveVersion_OnCreate_SendsTheDateAndSevenDays', async () => {
    const store = await setup();
    store.openVersionDialog(null);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());

    store.saveVersion(FORM);
    await vi.waitFor(() => expect(store.dialog()).toBeNull());

    const body = createVersion.mock.calls[0][1];
    expect(body.effectiveFrom).toBe('2027-07-05');
    expect(body.days).toHaveLength(7);
    expect(body.days[0]).toEqual({ weekday: 'monday', blocks: [{ start: '09:00', end: '13:00' }] });
    expect(store.dialog()).toBeNull();
  });

  it('saveVersion_OnEdit_SendsTheDaysAndTheRowVersionWithoutADate', async () => {
    const store = await setup();
    store.openVersionDialog(VERSION);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());

    store.saveVersion(FORM);

    expect(updateVersion).toHaveBeenCalledTimes(1);
    const [employeeId, versionId, body] = updateVersion.mock.calls[0];
    expect([employeeId, versionId]).toEqual(['e1', 'v1']);
    expect(body.rowVersion).toBe(4);
    expect(body.days).toHaveLength(7);
    expect(body).not.toHaveProperty('effectiveFrom');
  });

  it('saveVersion_With422AndWorkOrders_SetsTheAffectedListAndKeepsTheDialog', async () => {
    createVersion.mockReturnValue(throwError(affectedError));
    const store = await setup();
    store.openVersionDialog(null);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());

    store.saveVersion(FORM);

    expect(store.affectedWorkOrders()).toEqual([
      { id: 'wo-1', title: 'Replace filter', plannedStartLocal: '2027-06-10T09:00:00' },
    ]);
    expect(store.dialog()).not.toBeNull();
    expect(add).not.toHaveBeenCalled();
  });

  it('saveVersion_With422WithoutWorkOrders_ShowsTheRuleToast', async () => {
    createVersion.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity, error: {} })),
    );
    const store = await setup();
    store.openVersionDialog(null);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());

    store.saveVersion(FORM);

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'SHIFTS.TOASTS.RULE' }));
    expect(store.affectedWorkOrders()).toEqual([]);
    expect(store.dialog()).not.toBeNull();
  });

  it('saveVersion_With409_ShowsTheConflictToastClosesTheDialogAndReloads', async () => {
    updateVersion.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup();
    store.openVersionDialog(VERSION);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());
    const before = shiftVersions.mock.calls.length;

    store.saveVersion(FORM);
    await settle();

    await vi.waitFor(() => expect(store.dialog()).toBeNull());
    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'SHIFTS.TOASTS.CONFLICT' }));
    expect(shiftVersions.mock.calls.length - before).toBe(1);
  });

  it('saveOverride_OnEdit_SendsTheBlocksAndTheRowVersion', async () => {
    const store = await setup();
    store.openOverrideDialog(OVERRIDE);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());

    store.saveOverride({ date: Temporal.PlainDate.from('2027-06-10'), blocks: [] });

    expect(setOverride).toHaveBeenCalledWith('e1', '2027-06-10', { blocks: [], rowVersion: 2 });
  });

  it('saveOverride_OnCreate_SendsNoRowVersion', async () => {
    const store = await setup();
    store.openOverrideDialog(null);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());

    store.saveOverride({ date: Temporal.PlainDate.from('2027-06-12'), blocks: [{ start: '10:00', end: '11:00' }] });

    expect(setOverride.mock.calls[0][2]).toEqual({ blocks: [{ start: '10:00', end: '11:00' }] });
  });

  it('requestDeleteVersion_WhenConfirmed_DeletesTheVersion', async () => {
    const store = await setup({ isConfirmed: true });

    store.requestDeleteVersion(VERSION);

    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ acceptButtonProps: { severity: 'danger' } }));
    expect(deleteVersion).toHaveBeenCalledWith('e1', 'v1', VERSION.rowVersion);
  });

  it('requestDeleteOverride_WhenConfirmed_SendsTheRowVersionOfTheOverride', async () => {
    const store = await setup({ isConfirmed: true });

    store.requestDeleteOverride(OVERRIDE);

    expect(deleteOverride).toHaveBeenCalledWith('e1', '2027-06-10', OVERRIDE.rowVersion);
  });

  it('requestDeleteVersion_With409_ShowsTheConflictToastAndReloads', async () => {
    deleteVersion.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup({ isConfirmed: true });
    const before = shiftVersions.mock.calls.length;

    store.requestDeleteVersion(VERSION);
    await settle();

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'SHIFTS.TOASTS.CONFLICT' }));
    expect(shiftVersions.mock.calls.length - before).toBe(1);
  });

  it('requestDeleteOverride_WhenNotConfirmed_CallsNoEndpoint', async () => {
    const store = await setup({ isConfirmed: false });

    store.requestDeleteOverride(OVERRIDE);

    expect(deleteOverride).not.toHaveBeenCalled();
  });

  it('requestDeleteOverride_With422AndWorkOrders_KeepsTheAffectedListAndNoToast', async () => {
    deleteOverride.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: HttpStatusCode.UnprocessableEntity,
            error: {
              workOrders: [
                { id: 'wo-1', title: 'Replace filter', plannedStartLocal: '2027-06-10T09:00:00' },
                { id: 'wo-2', title: 'Check seal', plannedStartLocal: '2027-06-10T11:00:00' },
              ],
            },
          }),
      ),
    );
    const store = await setup({ isConfirmed: true });

    store.requestDeleteOverride(OVERRIDE);
    await settle();

    expect(store.dialog()).toBeNull();
    expect(store.affectedWorkOrders()).toHaveLength(2);
    expect(add).not.toHaveBeenCalled();
  });

  it('requestDeleteOverride_With422WithoutWorkOrders_ShowsTheRuleToast', async () => {
    deleteOverride.mockReturnValue(
      throwError(() => new HttpErrorResponse({ status: HttpStatusCode.UnprocessableEntity, error: {} })),
    );
    const store = await setup({ isConfirmed: true });

    store.requestDeleteOverride(OVERRIDE);
    await settle();

    expect(store.affectedWorkOrders()).toEqual([]);
    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'SHIFTS.TOASTS.RULE' }));
  });

  it('openOverrideDialog_ForANewOverride_LoadsTheBlocksOfTomorrow', async () => {
    const store = await setup();

    store.openOverrideDialog(null);
    await vi.waitFor(() => expect(store.prefillBlocks()).toEqual([{ start: '08:00', end: '17:00' }]));

    expect(daySchedule).toHaveBeenCalledWith('e1', '2027-06-02');
  });

  it('setPrefillDate_WithAnotherDate_LoadsTheBlocksOfThatDate', async () => {
    const store = await setup();
    store.openOverrideDialog(null);
    await vi.waitFor(() => expect(store.prefillBlocks()).not.toBeNull());

    store.setPrefillDate(Temporal.PlainDate.from('2027-06-05'));

    await vi.waitFor(() => expect(daySchedule).toHaveBeenLastCalledWith('e1', '2027-06-05'));
  });

  it('openOverrideDialog_ForANewOverrideOnADateWithAnOverride_BlocksTheDateAndKeepsTheBlocksEmpty', async () => {
    daySchedule.mockReturnValue(
      of({
        date: '2027-06-02',
        timeZoneId: 'Europe/Kyiv',
        source: DayScheduleSource.Override,
        blocks: [{ startLocal: '08:00:00' }],
        workOrders: [],
      }),
    );
    const store = await setup();

    store.openOverrideDialog(null);
    await vi.waitFor(() => expect(store.prefillBlocks()).toEqual([]));

    expect(store.isDateTaken()).toBe(true);
  });

  it('setPrefillDate_FromATakenDateToAFreeDate_ClearsTheBlock', async () => {
    daySchedule.mockReturnValueOnce(
      of({
        date: '2027-06-02',
        timeZoneId: 'Europe/Kyiv',
        source: DayScheduleSource.Override,
        blocks: [],
        workOrders: [],
      }),
    );
    const store = await setup();
    store.openOverrideDialog(null);
    await vi.waitFor(() => expect(store.isDateTaken()).toBe(true));

    store.setPrefillDate(Temporal.PlainDate.from('2027-06-05'));

    await vi.waitFor(() => expect(store.prefillBlocks()).toEqual([{ start: '08:00', end: '17:00' }]));
    expect(store.isDateTaken()).toBe(false);
  });

  it('setPrefillDate_WithTheDateInUse_LoadsNothingAgain', async () => {
    const store = await setup();
    store.openOverrideDialog(null);
    await vi.waitFor(() => expect(store.prefillBlocks()).not.toBeNull());
    const before = daySchedule.mock.calls.length;

    store.setPrefillDate(Temporal.PlainDate.from('2027-06-02'));
    await settle();

    expect(daySchedule.mock.calls.length).toBe(before);
  });

  it('openOverrideDialog_ForAStoredOverride_LoadsNoDay', async () => {
    const store = await setup();

    store.openOverrideDialog(OVERRIDE);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());
    await settle();

    expect(daySchedule).not.toHaveBeenCalled();
    expect(store.prefillBlocks()).toBeNull();
  });

  it('isEditable_ForAVersionThatTookEffect_IsFalse', async () => {
    const store = await setup();

    expect(store.isEditable(VERSION)).toBe(true);
    expect(store.isEditable({ ...VERSION, effectiveFrom: '2027-06-01' })).toBe(false);
    expect(store.isEditable({ ...VERSION, effectiveFrom: '2027-05-01' })).toBe(false);
  });

  it('today_AfterMidnightInTheCompanyZone_MovesToTheNextDayWithinOneMinute', async () => {
    vi.useFakeTimers({ toFake: ['setTimeout', 'setInterval', 'clearInterval', 'clearTimeout'] });
    currentNow = Temporal.Instant.from('2027-06-01T20:59:30Z');
    const store = await setup({ isConfirmed: false });
    const todayOverride = { ...OVERRIDE, date: '2027-06-01' };
    const nextDayOverride = { ...OVERRIDE, date: '2027-06-02' };
    const todayBefore = store.today().toString();
    const isTodayEditableBefore = store.isOverrideEditable(todayOverride);

    currentNow = Temporal.Instant.from('2027-06-01T21:00:00Z');
    await vi.advanceTimersByTimeAsync(30_000);
    store.requestDeleteOverride(nextDayOverride);

    expect(todayBefore).toBe('2027-06-01');
    expect(isTodayEditableBefore).toBe(true);
    expect(store.today().toString()).toBe('2027-06-02');
    expect(store.isOverrideEditable(todayOverride)).toBe(false);
    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ message: 'SHIFTS.CONFIRM.DELETE_TODAY_MESSAGE' }));
  });

  it('isOverrideEditable_ForAPastOverride_IsFalseAndForTodayAndLaterIsTrue', async () => {
    const store = await setup();

    expect(store.isOverrideEditable({ ...OVERRIDE, date: '2027-05-31' })).toBe(false);
    expect(store.isOverrideEditable({ ...OVERRIDE, date: '2027-06-01' })).toBe(true);
    expect(store.isOverrideEditable(OVERRIDE)).toBe(true);
  });

  it('requestDeleteOverride_ForToday_AsksWithTheTodayMessage', async () => {
    const store = await setup({ isConfirmed: false });

    store.requestDeleteOverride({ ...OVERRIDE, date: '2027-06-01' });

    expect(confirm).toHaveBeenCalledWith(expect.objectContaining({ message: 'SHIFTS.CONFIRM.DELETE_TODAY_MESSAGE' }));
  });

  it('requestDeleteOverride_ForALaterDate_AsksWithTheTemplateMessage', async () => {
    const store = await setup({ isConfirmed: false });

    store.requestDeleteOverride(OVERRIDE);

    expect(confirm).toHaveBeenCalledWith(
      expect.objectContaining({ message: 'SHIFTS.CONFIRM.DELETE_OVERRIDE_MESSAGE' }),
    );
  });

  it('openOverrideDialog_AfterAFailedVersionSave_ClearsTheAffectedList', async () => {
    createVersion.mockReturnValue(throwError(affectedError));
    const store = await setup();
    store.openVersionDialog(null);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());
    store.saveVersion(FORM);
    expect(store.affectedWorkOrders()).toHaveLength(1);

    store.openOverrideDialog(null);

    await vi.waitFor(() => expect(store.dialog()?.kind).toBe('override'));
    await settle();
    expect(store.affectedWorkOrders()).toEqual([]);
  });

  it('openWorkOrder_WhileShiftDialogIsOpen_KeepsDialogAndAffected', async () => {
    createVersion.mockReturnValue(throwError(affectedError));
    const store = await setup({ isActionStoreReal: true });
    store.openVersionDialog(null);
    await vi.waitFor(() => expect(store.dialog()).not.toBeNull());
    store.saveVersion(FORM);
    const navigation = TestBed.inject(PlatformNavigation) as FakePlatformNavigation;
    const entryCount = navigation.entries().length;

    store.openWorkOrder('wo-1');
    await vi.waitFor(() => expect(TestBed.inject(ActionStore).active()).not.toBeNull());

    expect(TestBed.inject(ActionStore).active()).toEqual({
      type: WorkOrderActionType.Open,
      payload: { workOrderId: 'wo-1' },
    });
    expect(store.dialog()).not.toBeNull();
    expect(store.affectedWorkOrders()).toHaveLength(1);
    expect(navigation.entries().length).toBe(entryCount);
  });

  it('nextMonth_AndPreviousMonth_ReloadTheOverridesOfThatMonth', async () => {
    const store = await setup();

    store.nextMonth();
    await settle();

    expect(shiftOverrides).toHaveBeenLastCalledWith('e1', '2027-07-01', '2027-07-31');
  });
});

describe('toShiftDialog', () => {
  it.each([
    ['WithoutCompose', null, null],
    ['WithTheNewVersion', 'shift-version', { kind: 'version', version: null }],
    ['WithAStoredVersion', 'shift-version:v1', { kind: 'version', version: VERSION }],
    ['WithAnUnknownVersion', 'shift-version:v9', 'unknown'],
    ['WithTheNewOverride', 'shift-override', { kind: 'override', override: null }],
    ['WithAStoredOverride', 'shift-override:o1', { kind: 'override', override: OVERRIDE }],
    ['WithAnUnknownOverride', 'shift-override:o9', 'unknown'],
    ['WithAnotherName', 'phase:p1', null],
  ])('toShiftDialog_%s', (_name, compose, expected) => {
    const result = toShiftDialog(compose, [VERSION], [OVERRIDE]);

    expect(result).toEqual(expected);
  });

  it('toShiftDialog_WithAStoredIdBeforeTheListLoads_IsNull', () => {
    expect(toShiftDialog('shift-version:v1', null, null)).toBeNull();
    expect(toShiftDialog('shift-override:o1', null, null)).toBeNull();
  });
});

describe('shift utils', () => {
  it('normalizeVersion_WithSecondsAndMissingDays_CutsThemAndFillsSevenDays', () => {
    expect(VERSION.days).toHaveLength(7);
    expect(VERSION.days[0].blocks).toEqual([{ start: '08:00', end: '12:00' }]);
    expect(VERSION.days[1].blocks).toEqual([]);
  });

  it('toVersionFormValue_ForAnExistingVersion_KeepsItsDateSoTheFormIsValid', () => {
    const value = toVersionFormValue(VERSION, Temporal.PlainDate.from('2027-06-01'));

    expect(value.effectiveFrom?.toString()).toBe('2027-07-01');
    expect(versionFormSchema['~standard'].validate(value)).not.toHaveProperty('issues');
  });

  it('monthRange_WithFebruaryOfALeapYear_Ends29', () => {
    const range = monthRange(Temporal.PlainDate.from('2028-02-10'));

    expect(range).toEqual({ from: '2028-02-01', to: '2028-02-29' });
  });

  it('affectedOf_WithAnotherStatus_IsNull', () => {
    const result = affectedOf(new HttpErrorResponse({ status: HttpStatusCode.Conflict, error: { workOrders: [] } }));

    expect(result).toBeNull();
  });
});

describe('isOverrideDateTaken', () => {
  it.each([
    ['CreateOnADateWithAnOverride', true, DayScheduleSource.Override, true],
    ['EditOfAnOverride', false, DayScheduleSource.Override, false],
    ['CreateOnATemplateDay', true, DayScheduleSource.Template, false],
    ['CreateBeforeTheDayLoads', true, null, false],
  ])('isOverrideDateTaken_For%s_Decides', (_name, isCreate, source, expected) => {
    expect(isOverrideDateTaken(isCreate, source)).toBe(expected);
  });
});
