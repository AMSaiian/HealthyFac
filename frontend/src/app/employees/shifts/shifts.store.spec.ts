import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { ApplicationRef } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';

import { ActionStore } from '@shared/actions/action.store';
import { AuthStore } from '@shared/security/auth.store';
import { CLOCK } from '@shared/time/clock';
import { Temporal } from '@shared/time/temporal';

import { EmployeesApi } from '@employees/employees-api';
import { type ShiftOverride, type ShiftVersion, Weekday } from '@employees/shifts/shift';
import {
  affectedOf,
  emptyDays,
  monthRange,
  normalizeVersion,
  toVersionFormValue,
} from '@employees/shifts/shifts.store.utils';
import { versionFormSchema } from '@employees/shifts/version-dialog/version-dialog-schema';
import type { VersionFormValue } from '@employees/shifts/version-dialog/version-dialog-schema';

import { ShiftsStore } from './shifts.store';

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
  const add = vi.fn();
  const confirm = vi.fn();

  async function settle(): Promise<void> {
    TestBed.tick();
    await TestBed.inject(ApplicationRef).whenStable();
  }

  async function setup(options: { isConfirmed?: boolean } = {}): Promise<InstanceType<typeof ShiftsStore>> {
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
        { provide: AuthStore, useValue: { hasPermission: () => true } },
        { provide: ActionStore, useValue: { dispatch: vi.fn() } },
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
          useValue: { now: () => Temporal.Instant.from('2027-06-01T09:00:00Z'), timeZoneId: () => 'Europe/Kyiv' },
        },
      ],
    });
    const store = TestBed.inject(ShiftsStore);
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
    add.mockReset();
    confirm.mockClear();
  });

  it('setEmployee_OnFirstLoad_ReadsTheVersionsAndTheOverridesOfTheCurrentMonth', async () => {
    await setup();

    expect(shiftVersions).toHaveBeenCalledWith('e1');
    expect(shiftOverrides).toHaveBeenCalledWith('e1', '2027-06-01', '2027-06-30');
  });

  it('saveVersion_OnCreate_SendsTheDateAndSevenDays', async () => {
    const store = await setup();
    store.openVersionDialog(null);

    store.saveVersion(FORM);

    const body = createVersion.mock.calls[0][1];
    expect(body.effectiveFrom).toBe('2027-07-05');
    expect(body.days).toHaveLength(7);
    expect(body.days[0]).toEqual({ weekday: 'monday', blocks: [{ start: '09:00', end: '13:00' }] });
    expect(store.dialog()).toBeNull();
  });

  it('saveVersion_OnEdit_SendsTheDaysAndTheRowVersionWithoutADate', async () => {
    const store = await setup();
    store.openVersionDialog(VERSION);

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

    store.saveVersion(FORM);

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'SHIFTS.TOASTS.RULE' }));
    expect(store.affectedWorkOrders()).toEqual([]);
    expect(store.dialog()).not.toBeNull();
  });

  it('saveVersion_With409_ShowsTheConflictToastClosesTheDialogAndReloads', async () => {
    updateVersion.mockReturnValue(throwError(() => new HttpErrorResponse({ status: HttpStatusCode.Conflict })));
    const store = await setup();
    store.openVersionDialog(VERSION);
    const before = shiftVersions.mock.calls.length;

    store.saveVersion(FORM);
    await settle();

    expect(add).toHaveBeenCalledWith(expect.objectContaining({ detail: 'SHIFTS.TOASTS.CONFLICT' }));
    expect(store.dialog()).toBeNull();
    expect(shiftVersions.mock.calls.length - before).toBe(1);
  });

  it('saveOverride_OnEdit_SendsTheBlocksAndTheRowVersion', async () => {
    const store = await setup();
    store.openOverrideDialog(OVERRIDE);

    store.saveOverride({ date: Temporal.PlainDate.from('2027-06-10'), blocks: [] });

    expect(setOverride).toHaveBeenCalledWith('e1', '2027-06-10', { blocks: [], rowVersion: 2 });
  });

  it('saveOverride_OnCreate_SendsNoRowVersion', async () => {
    const store = await setup();
    store.openOverrideDialog(null);

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

  it('isEditable_ForAVersionThatTookEffect_IsFalse', async () => {
    const store = await setup();

    expect(store.isEditable(VERSION)).toBe(true);
    expect(store.isEditable({ ...VERSION, effectiveFrom: '2027-06-01' })).toBe(false);
    expect(store.isEditable({ ...VERSION, effectiveFrom: '2027-05-01' })).toBe(false);
  });

  it('nextMonth_AndPreviousMonth_ReloadTheOverridesOfThatMonth', async () => {
    const store = await setup();

    store.nextMonth();
    await settle();

    expect(shiftOverrides).toHaveBeenLastCalledWith('e1', '2027-07-01', '2027-07-31');
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
