import type { ComposeCodec } from '@shared/actions/app-action';
import { hasOnlyStringKeys } from '@shared/actions/compose-codec';
import { formatCompose } from '@shared/history/history.store.utils';

export const MachineActionType = {
  Create: 'machines.create',
  Update: 'machines.update',
  Drafts: 'machines.drafts',
  ChangeState: 'machines.change-state',
  Saved: 'machines.saved',
} as const;

export const MACHINE_COMPOSE_NAME = 'machine';
export const MACHINE_DRAFTS_COMPOSE_NAME = 'machine-drafts';

/**
 * `machine` opens the create drawer, `machine:<id>` the update drawer. A create drawer with a draft or prefill fields
 * stays in memory. One codec carries both types, because both share the name.
 */
export const MACHINE_COMPOSE: ComposeCodec = {
  name: MACHINE_COMPOSE_NAME,
  encode: (action) => {
    if (action.type === MachineActionType.Create && hasOnlyStringKeys(action.payload, [])) {
      return formatCompose(MACHINE_COMPOSE_NAME);
    }
    if (
      action.type === MachineActionType.Update &&
      hasOnlyStringKeys(action.payload, ['machineId']) &&
      action.payload['machineId'] !== undefined
    ) {
      return formatCompose(MACHINE_COMPOSE_NAME, action.payload['machineId']);
    }
    return null;
  },
  decode: (id) =>
    id === null
      ? { type: MachineActionType.Create, payload: {} }
      : { type: MachineActionType.Update, payload: { machineId: id } },
};

export const MACHINE_DRAFTS_COMPOSE: ComposeCodec = {
  name: MACHINE_DRAFTS_COMPOSE_NAME,
  encode: (action) =>
    action.type === MachineActionType.Drafts &&
    hasOnlyStringKeys(action.payload, ['machineId']) &&
    action.payload['machineId'] !== undefined
      ? formatCompose(MACHINE_DRAFTS_COMPOSE_NAME, action.payload['machineId'])
      : null,
  decode: (id) => (id === null ? null : { type: MachineActionType.Drafts, payload: { machineId: id } }),
};
