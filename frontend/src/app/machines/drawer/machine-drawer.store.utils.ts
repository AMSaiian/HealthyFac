import type { AppAction } from '@shared/actions/app-action';

import type { Machine } from '@machines/machine';

import type { MachineActionPayload, MachineFormValue } from './machine-form-schema';

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

export function overlay(base: MachineFormValue, payload: MachineActionPayload): MachineFormValue {
  return {
    name: payload.name ?? base.name,
    serialNumber: payload.serialNumber ?? base.serialNumber,
    departmentId: payload.departmentId ?? base.departmentId,
    description: payload.description === undefined ? base.description : payload.description,
  };
}
