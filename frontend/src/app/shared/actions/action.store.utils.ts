import type { AppAction } from './app-action';

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

/** The entry state of the action store: `{ active, parent }`. */
export function isEntryState(value: unknown): value is { active: unknown; parent: unknown } {
  return isRecord(value) && 'active' in value && 'parent' in value;
}

/** Reads an action from an untrusted value. Returns `null` when the value has no action shape. */
export function toAction(value: unknown): AppAction | null {
  return isRecord(value) && typeof value['type'] === 'string' && 'payload' in value
    ? { type: value['type'], payload: value['payload'] }
    : null;
}
