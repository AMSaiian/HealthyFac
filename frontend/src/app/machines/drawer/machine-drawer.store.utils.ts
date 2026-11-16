import type { AppAction } from '@shared/actions/app-action';
import { Temporal } from '@shared/time/temporal';

import type { MachineDraftDetail } from '@machines/drafts/draft';
import type { Machine, MachineBody, MachineState, UpdateMachineBody } from '@machines/machine';

import type { MachineActionPayload, MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

/** The form value of a new machine: the go-live date starts at `today` in the company time zone. */
export function emptyValue(today: Temporal.PlainDate): MachineFormValue {
  return { name: '', serialNumber: '', description: null, expectedGoLiveOn: today, state: null };
}

export function toFormValue(machine: Machine): MachineFormValue {
  return {
    name: machine.name,
    serialNumber: machine.serialNumber,
    description: machine.description,
    expectedGoLiveOn: Temporal.PlainDate.from(machine.expectedGoLiveOn),
    state: machine.state,
  };
}

/** The form value of a create draft: a missing (`null`) field starts empty, and the date starts at `today`. */
export function fromDraftValue(draft: MachineDraftDetail, today: Temporal.PlainDate): MachineFormValue {
  return {
    name: draft.name ?? '',
    serialNumber: draft.serialNumber ?? '',
    description: draft.description,
    expectedGoLiveOn: today,
    state: null,
  };
}

export function payloadOf(action: AppAction | null): MachineActionPayload {
  return (action?.payload ?? {}) as MachineActionPayload;
}

/** The body of a valid form: **Save** stays disabled while the go-live date is missing. */
export function toBody(value: MachineFormValue): MachineBody {
  return {
    name: value.name.trim(),
    serialNumber: value.serialNumber.trim(),
    description: value.description,
    expectedGoLiveOn: String(value.expectedGoLiveOn),
  };
}

/** The body of an update: the create body and the state of the form. */
export function toUpdateBody(value: MachineFormValue, state: MachineState): UpdateMachineBody {
  return { ...toBody(value), state };
}

export function overlay(base: MachineFormValue, payload: MachineActionPayload): MachineFormValue {
  return {
    name: payload.name ?? base.name,
    serialNumber: payload.serialNumber ?? base.serialNumber,
    description: payload.description === undefined ? base.description : payload.description,
    expectedGoLiveOn: base.expectedGoLiveOn,
    state: base.state,
  };
}

/** The first day that the go-live picker allows: today on create, the creation day on edit. */
export function minGoLiveOn(
  machine: Machine | null,
  today: Temporal.PlainDate,
  timeZoneId: string,
): Temporal.PlainDate {
  return machine === null
    ? today
    : Temporal.Instant.from(machine.createdAt).toZonedDateTimeISO(timeZoneId).toPlainDate();
}
