import type { AppAction } from '@shared/actions/app-action';
import type { MachineState } from '@machines/machine';
import type { MachineStateActionPayload } from '@machines/state/machine-state-action-payloads';

export const NO_CHANGES_REASON = 'FORMS.NO_CHANGES';

export function machineIdOf(action: AppAction | null): string | undefined {
  return (action?.payload as Partial<MachineStateActionPayload> | undefined)?.machineId;
}

/** **Save** stays disabled while the selected state is the current state, and while the machine is not loaded. */
export function saveDisabledReason(
  currentState: MachineState | null,
  selectedState: MachineState | null,
): string | null {
  return selectedState === null || selectedState === currentState ? NO_CHANGES_REASON : null;
}
