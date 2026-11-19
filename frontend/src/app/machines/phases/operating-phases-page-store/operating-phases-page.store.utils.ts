import type { OperatingPhase } from '@machines/phases/operating-phase';

/** The editor of the page: a new phase (`phaseId` is `null`) or a stored one. */
export interface PhaseEditor {
  readonly phaseId: string | null;
}

/** The phases in the order that the evaluator reads them and that the reorder API stores: the lowest priority first. */
export function sortByPriority(phases: readonly OperatingPhase[]): readonly OperatingPhase[] {
  return [...phases].sort((first, second) => first.priority - second.priority);
}

/**
 * The IDs in the order after a row moved. `from` and `to` are the row positions before and after the move.
 * A position outside the list keeps the order.
 */
export function movedOrder(phases: readonly OperatingPhase[], from: number, to: number): readonly string[] {
  const ids = phases.map((phase) => phase.id);
  if (from < 0 || from >= ids.length || to < 0 || to >= ids.length || from === to) {
    return ids;
  }
  const [moved] = ids.splice(from, 1);
  ids.splice(to, 0, moved);
  return ids;
}
