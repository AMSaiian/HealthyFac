import type { ComposeCodec } from '@shared/actions/app-action';
import { formatCompose } from '@shared/history/history.store.utils';

export const FileActionType = {
  Upload: 'files.upload',
  Edit: 'files.edit',
  Tie: 'files.tie',
  IngestHistory: 'files.ingestHistory',
  IngestSummary: 'files.ingestSummary',
  Saved: 'files.saved',
} as const;

export const FILE_COMPOSE_NAME = 'file';

/** `file:<id>` opens the edit dialog. The scope comes from the route param `machineId`: set for a machine, else the company. */
export const FILE_EDIT_COMPOSE: ComposeCodec = {
  name: FILE_COMPOSE_NAME,
  encode: (action) => {
    const payload = action.payload as { id?: unknown } | null;
    return action.type === FileActionType.Edit && typeof payload?.id === 'string'
      ? formatCompose(FILE_COMPOSE_NAME, payload.id)
      : null;
  },
  decode: (id, params) => {
    const machineId = params['machineId'];
    return id === null
      ? null
      : {
          type: FileActionType.Edit,
          payload: { scope: machineId === undefined ? { kind: 'company' } : { kind: 'machine', machineId }, id },
        };
  },
};
