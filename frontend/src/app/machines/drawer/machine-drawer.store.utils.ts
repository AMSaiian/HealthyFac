import type { AppAction } from '@shared/actions/app-action';

import type { MachineDraftDetail } from '@machines/drafts/draft';
import type { Machine, MachineBody } from '@machines/machine';

import type { MachineActionPayload, MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

export const EMPTY_VALUE: MachineFormValue = { name: '', serialNumber: '', description: null };

export function toFormValue(machine: Machine): MachineFormValue {
  return {
    name: machine.name,
    serialNumber: machine.serialNumber,
    description: machine.description,
  };
}

/** The form value of a create draft: a missing (`null`) field starts empty. */
export function fromDraftValue(draft: MachineDraftDetail): MachineFormValue {
  return {
    name: draft.name ?? '',
    serialNumber: draft.serialNumber ?? '',
    description: draft.description,
  };
}

export function payloadOf(action: AppAction | null): MachineActionPayload {
  return (action?.payload ?? {}) as MachineActionPayload;
}

export function toBody(value: MachineFormValue): MachineBody {
  return {
    name: value.name.trim(),
    serialNumber: value.serialNumber.trim(),
    description: value.description,
  };
}

export function overlay(base: MachineFormValue, payload: MachineActionPayload): MachineFormValue {
  return {
    name: payload.name ?? base.name,
    serialNumber: payload.serialNumber ?? base.serialNumber,
    description: payload.description === undefined ? base.description : payload.description,
  };
}
