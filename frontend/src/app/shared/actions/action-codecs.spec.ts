import { APP_ACTION_HOSTS } from '@app/app-action-hosts';
import {
  SHIFT_OVERRIDE_COMPOSE_NAME,
  SHIFT_VERSION_COMPOSE_NAME,
} from '@employees/shifts/shifts-store/shifts.store.utils';
import { PHASE_COMPOSE_NAME } from '@machines/phases/operating-phases-page-store/operating-phases-page.store.utils';
import { STATUS_COMPOSE_NAME } from '@machines/statuses/list/status-definitions-grid.store.utils';
import { FILE_EDIT_COMPOSE, FileActionType } from '@files/file-actions';
import { MACHINE_COMPOSE, MACHINE_DRAFTS_COMPOSE, MachineActionType } from '@machines/machine-actions';
import { WORK_ORDER_COMPOSE, WorkOrderActionType } from '@work-orders/work-order-actions';

describe('compose codecs', () => {
  it.each([
    ['work-order', WORK_ORDER_COMPOSE, { type: WorkOrderActionType.Open, payload: {} }, 'work-order', null],
    [
      'work-order:w1',
      WORK_ORDER_COMPOSE,
      { type: WorkOrderActionType.Open, payload: { workOrderId: 'w1' } },
      'work-order:w1',
      'w1',
    ],
    ['machine', MACHINE_COMPOSE, { type: MachineActionType.Create, payload: {} }, 'machine', null],
    [
      'machine:m1',
      MACHINE_COMPOSE,
      { type: MachineActionType.Update, payload: { machineId: 'm1' } },
      'machine:m1',
      'm1',
    ],
    [
      'machine-drafts:m1',
      MACHINE_DRAFTS_COMPOSE,
      { type: MachineActionType.Drafts, payload: { machineId: 'm1' } },
      'machine-drafts:m1',
      'm1',
    ],
  ])('codec_%s_EncodesAndDecodes', (_name, codec, action, value, id) => {
    const encoded = codec.encode(action);
    const decoded = codec.decode(id, {});

    expect(encoded).toBe(value);
    expect(decoded).toEqual(action);
  });

  it.each([
    ['WorkOrderWithPrefill', WORK_ORDER_COMPOSE, { type: WorkOrderActionType.Open, payload: { title: 'x' } }],
    ['MachineCreateWithDraft', MACHINE_COMPOSE, { type: MachineActionType.Create, payload: { draftId: 'd1' } }],
    ['MachineCreateWithPrefill', MACHINE_COMPOSE, { type: MachineActionType.Create, payload: { name: 'x' } }],
    [
      'MachineUpdateWithPrefill',
      MACHINE_COMPOSE,
      { type: MachineActionType.Update, payload: { machineId: 'm1', name: 'x' } },
    ],
    ['AnotherType', MACHINE_COMPOSE, { type: MachineActionType.ChangeState, payload: {} }],
  ])('encode_%s_ReturnsNull', (_name, codec, action) => {
    const encoded = codec.encode(action);

    expect(encoded).toBeNull();
  });

  it('decode_MachineDraftsWithoutId_ReturnsNull', () => {
    const decoded = MACHINE_DRAFTS_COMPOSE.decode(null, {});

    expect(decoded).toBeNull();
  });

  it.each([
    ['Company', {}, { kind: 'company' }],
    ['Machine', { machineId: 'm1' }, { kind: 'machine', machineId: 'm1' }],
  ])('decode_FileEditFor%sRoute_TakesTheScopeFromTheRouteParams', (_name, params, scope) => {
    const decoded = FILE_EDIT_COMPOSE.decode('f1', params);

    expect(decoded).toEqual({ type: FileActionType.Edit, payload: { scope, id: 'f1' } });
  });

  it('encode_FileEdit_UsesTheFileId', () => {
    const encoded = FILE_EDIT_COMPOSE.encode({
      type: FileActionType.Edit,
      payload: { scope: { kind: 'company' }, id: 'f1' },
    });

    expect(encoded).toBe('file:f1');
  });

  it('decode_FileEditWithoutId_ReturnsNull', () => {
    const decoded = FILE_EDIT_COMPOSE.decode(null, {});

    expect(decoded).toBeNull();
  });

  it('composeNames_AreUnique', () => {
    const codecs = new Set(APP_ACTION_HOSTS.flatMap((host) => (host.compose === undefined ? [] : [host.compose])));

    const names = [
      ...[...codecs].map((codec) => codec.name),
      STATUS_COMPOSE_NAME,
      PHASE_COMPOSE_NAME,
      SHIFT_VERSION_COMPOSE_NAME,
      SHIFT_OVERRIDE_COMPOSE_NAME,
    ];

    expect(new Set(names).size).toBe(names.length);
  });
});
