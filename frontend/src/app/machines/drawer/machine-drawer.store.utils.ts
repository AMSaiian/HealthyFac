import type { AppAction } from '@shared/actions/app-action';

import type { Machine, MachineUpdateBody } from '@machines/machine';

import type { MachineActionPayload, MachineFormValue } from '@machines/drawer/machine-form/machine-form-schema';

export const EMPTY_VALUE: MachineFormValue = { name: '', serialNumber: '', departmentId: '', description: null };

export function toFormValue(machine: Machine): MachineFormValue {
  return {
    name: machine.name,
    serialNumber: machine.serialNumber,
    departmentId: machine.departmentId,
    description: machine.description,
  };
}

export function payloadOf(action: AppAction | null): MachineActionPayload {
  return (action?.payload ?? {}) as MachineActionPayload;
}

export function toUpdateBody(value: MachineFormValue): MachineUpdateBody {
  return {
    name: value.name.trim(),
    serialNumber: value.serialNumber.trim(),
    description: value.description,
  };
}

/** The department is read-only in an update, so the payload never overrides it. */
export function overlay(base: MachineFormValue, payload: MachineActionPayload, isUpdate: boolean): MachineFormValue {
  return {
    name: payload.name ?? base.name,
    serialNumber: payload.serialNumber ?? base.serialNumber,
    departmentId: isUpdate ? base.departmentId : (payload.departmentId ?? base.departmentId),
    description: payload.description === undefined ? base.description : payload.description,
  };
}
